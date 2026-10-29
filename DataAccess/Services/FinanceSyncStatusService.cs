using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using InvEntry.Contracts.FinanceSync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.Common;
using System.Text.Json;

namespace DataAccess.Services;

/// <summary>
/// Builds the user-facing synchronization status: what is not (fully) in
/// FinanceTracker and what to do, plus the setup and service problems only an
/// administrator can fix. Sends events again on request.
/// </summary>
public sealed class FinanceSyncStatusService
{
    /// <summary>Synchronized events are checked for FinanceTracker suspense this far back.</summary>
    public static readonly TimeSpan SuspenseLookBack = TimeSpan.FromDays(60);

    // Every payment mode and category the posting procedures use.
    private static readonly string[] RequiredModes =
        ["CASH", "UPI", "BANK", "CARD", "CHEQUE", "CREDIT", "ADJUSTMENT", "WRITE_OFF"];
    private static readonly (string Code, string Direction)[] RequiredCategories =
    [
        ("SALES_RECEIPT", "IN"), ("REFUND", "OUT"), ("CREDIT_SALE", "IN"),
        ("ADVANCE_ADJ", "IN"), ("RD_ADJ", "IN"), ("CREDIT_COLLECTION", "IN"),
        ("CREDIT_WRITE_OFF", "IN")
    ];

    private readonly MijmsContext _context;
    private readonly FinanceSyncOptions _options;
    private readonly FinanceSyncHeartbeat _heartbeat;
    private readonly FinanceSyncDispatchJobQueue _jobs;

    public FinanceSyncStatusService(MijmsContext context,
        IOptions<FinanceSyncOptions> options, FinanceSyncHeartbeat heartbeat,
        FinanceSyncDispatchJobQueue jobs)
    {
        _context = context;
        _options = options.Value;
        _heartbeat = heartbeat;
        _jobs = jobs;
    }

    public async Task<FinanceSyncStatusDto> GetAsync(CancellationToken token)
    {
        var nowLocal = DateTime.Now;
        var nowUtc = DateTime.UtcNow;
        var suspenseFrom = nowLocal - SuspenseLookBack;

        var rows = await _context.FinanceSyncOutboxes.AsNoTracking()
            .Where(x => x.Status != FinanceSyncService.Sent || x.CreatedOn >= suspenseFrom)
            .ToListAsync(token);

        var issues = new List<FinanceSyncIssueDto>();
        var suspense = await SuspenseEventIdsAsync(issues, token);
        await CheckSetupAsync(issues, token);
        var running = CheckAutomaticSync(issues, out var lastCycle);

        var items = rows
            .Select(row => (Row: row, Assessment: FinanceSyncAttention.Assess(row, nowLocal, nowUtc,
                suspense.Contains(row.SourceEventId))))
            .Where(x => x.Assessment != null)
            .Select(x => ToItem(x.Row, x.Assessment!))
            .OrderBy(x => FinanceSyncAttention.Priority(x.State))
            .ThenBy(x => x.DocumentDate)
            .ThenBy(x => x.EventId)
            .ToList();

        int Count(string state) => items.Count(x => x.State == state);
        var status = new FinanceSyncStatusDto
        {
            CheckedOn = nowLocal,
            AutomaticSyncRunning = running,
            LastAutomaticSyncOn = lastCycle?.ToLocalTime(),
            Issues = issues,
            InProgressCount = Count(FinanceSyncAttentionStates.InProgress),
            StuckCount = Count(FinanceSyncAttentionStates.Stuck),
            NeedsActionCount = Count(FinanceSyncAttentionStates.NeedsAction),
            NeedsAdminCount = Count(FinanceSyncAttentionStates.NeedsAdmin),
            InSuspenseCount = Count(FinanceSyncAttentionStates.InSuspense),
            SendableCount = items.Count(x => x.CanSend),
            Items = items
        };
        (status.OverallState, status.Headline) = Summarize(status);
        return status;
    }

    /// <summary>
    /// Sends the given events again, or every event that can be sent when none are
    /// given. Runs on the manual dispatch worker, so the call returns at once.
    /// </summary>
    public async Task<FinanceSyncSendResultDto> SendAsync(IReadOnlyCollection<long>? eventIds,
        string actor, CancellationToken token)
    {
        if (!_options.Enabled)
            return new() { Message = "Finance synchronization is switched off on the InvEntry server." };

        var status = await GetAsync(token);
        var sendable = status.Items.Where(x => x.CanSend).Select(x => x.EventId).ToHashSet();
        var ids = (eventIds is { Count: > 0 } ? eventIds.Where(sendable.Contains) : sendable)
            .Distinct().Take(100).ToList();

        if (ids.Count == 0)
            return new() { Message = "Nothing needs sending. Refresh to see the latest status." };

        try
        {
            var accepted = _jobs.Enqueue(ids, actor);
            return new()
            {
                Requested = ids.Count,
                JobId = accepted.JobId,
                Message = ids.Count == 1
                    ? "Sending 1 entry to FinanceTracker. Refresh in a minute to see the result."
                    : $"Sending {ids.Count} entries to FinanceTracker. Refresh in a minute to see the result."
            };
        }
        catch (InvalidOperationException)
        {
            return new() { Message = "A send is already in progress. Refresh in a minute." };
        }
    }

    private static (string State, string Headline) Summarize(FinanceSyncStatusDto status)
    {
        if (status.Issues.Count > 0 || status.NeedsAdminCount > 0)
            return (FinanceSyncOverallStates.Problem, status.Issues.Count > 0
                ? status.Issues[0].Message
                : $"{Entries(status.NeedsAdminCount)} need the administrator before they can reach FinanceTracker.");

        var action = status.StuckCount + status.NeedsActionCount + status.InSuspenseCount;
        if (action > 0)
            return (FinanceSyncOverallStates.Attention,
                $"{Entries(action)} need attention: {Breakdown(status)}.");

        return (FinanceSyncOverallStates.Ok, status.InProgressCount > 0
            ? $"Everything is reaching FinanceTracker; {Entries(status.InProgressCount)} being sent now."
            : "Everything is in FinanceTracker.");
    }

    private static string Breakdown(FinanceSyncStatusDto status)
    {
        var parts = new List<string>();
        if (status.StuckCount > 0) parts.Add($"{status.StuckCount} not sent");
        if (status.NeedsActionCount > 0) parts.Add($"{status.NeedsActionCount} to correct");
        if (status.InSuspenseCount > 0) parts.Add($"{status.InSuspenseCount} on hold in Suspense");
        return string.Join(", ", parts);
    }

    private static string Entries(int count) => count == 1 ? "1 entry" : $"{count} entries";

    private bool CheckAutomaticSync(List<FinanceSyncIssueDto> issues, out DateTime? lastCycle)
    {
        var (completed, failed, error) = _heartbeat.Snapshot();
        lastCycle = completed;

        if (!_options.Enabled)
        {
            issues.Add(new()
            {
                Code = "SYNC_DISABLED",
                Message = "Finance synchronization is switched off on the InvEntry server.",
                NextStep = "Ask the administrator to enable the FinanceTracker section of the InvEntry server settings."
            });
            return false;
        }

        if (failed > (completed ?? DateTime.MinValue))
        {
            issues.Add(new()
            {
                Code = "SYNC_FAILING",
                Message = "The automatic sync is failing, so new invoices are not reaching FinanceTracker.",
                NextStep = $"Ask the administrator to check the InvEntry server log. Last error: {Shorten(error, 200)}"
            });
            return false;
        }

        if (completed is null || DateTime.UtcNow - completed > FinanceSyncHeartbeat.StoppedAfter)
        {
            // Just started: the first cycle runs ten seconds after start-up.
            if (completed is null && failed is null)
                return true;

            issues.Add(new()
            {
                Code = "SYNC_STOPPED",
                Message = "The automatic sync has stopped, so new invoices are not reaching FinanceTracker.",
                NextStep = "Ask the administrator to restart the InvEntry server (DataAccess) service."
            });
            return false;
        }

        return true;
    }

    private async Task CheckSetupAsync(List<FinanceSyncIssueDto> issues, CancellationToken token)
    {
        try
        {
            var modes = await ReadStringsAsync("""
                SELECT MODE_CODE FROM FinanceTracker.dbo.PAYMENT_MODE
                WHERE ORG_GKEY = @org AND IS_ACTIVE = 1
                """, token);
            var categories = await ReadStringsAsync("""
                SELECT CONCAT(CATEGORY_CODE, '|', DIRECTION) FROM FinanceTracker.dbo.TRANSACTION_CATEGORY
                WHERE ORG_GKEY = @org AND IS_ACTIVE = 1
                """, token);

            var missing = RequiredModes.Where(x => !modes.Contains(x)).Select(x => $"payment mode {x}")
                .Concat(RequiredCategories.Where(x => !categories.Contains($"{x.Code}|{x.Direction}"))
                    .Select(x => $"category {x.Code}"))
                .ToList();
            if (missing.Count > 0)
                issues.Add(new()
                {
                    Code = "SETUP_INCOMPLETE",
                    Message = "FinanceTracker setup is incomplete, so some invoices and receipts cannot be posted.",
                    NextStep = $"Ask the administrator to run the FinanceTracker setup check script. Missing: {string.Join(", ", missing)}."
                });

            var constraint = await ReadStringsAsync("""
                SELECT definition FROM sys.check_constraints
                WHERE name = 'CK_FINANCE_SYNC_OUTBOX_STATUS'
                  AND parent_object_id = OBJECT_ID('dbo.FINANCE_SYNC_OUTBOX')
                """, token);
            if (constraint.Count > 0 && !constraint.Any(x => x.Contains(FinanceSyncService.RetryableError)))
                issues.Add(new()
                {
                    Code = "DATABASE_OUT_OF_DATE",
                    Message = "The InvEntry database is out of date: a failed sync cannot be recorded and can block the rest.",
                    NextStep = "Ask the administrator to apply the latest InvEntry database update (outbox status constraint)."
                });
        }
        catch (DbException)
        {
            issues.Add(new()
            {
                Code = "CHECK_UNAVAILABLE",
                Message = "The FinanceTracker setup could not be checked.",
                NextStep = "Refresh. If this persists, ask the administrator to check the database connection."
            });
        }
    }

    private async Task<HashSet<string>> SuspenseEventIdsAsync(List<FinanceSyncIssueDto> issues,
        CancellationToken token)
    {
        try
        {
            return await ReadStringsAsync("""
                SELECT DISTINCT D.SOURCE_EVENT_ID
                FROM FinanceTracker.dbo.FIN_DOCUMENT D
                JOIN FinanceTracker.dbo.FIN_TRANSACTION T ON T.DOCUMENT_GKEY = D.GKEY
                WHERE D.ORG_GKEY = @org
                  AND D.LOCATION_GKEY = @location
                  AND D.SOURCE_SYSTEM = 'INVENTRY'
                  AND T.IS_SUSPENSE = 1
                  AND T.STATUS = 'POSTED'
                """, token);
        }
        catch (DbException)
        {
            issues.Add(new()
            {
                Code = "CHECK_UNAVAILABLE",
                Message = "FinanceTracker could not be checked for entries on hold.",
                NextStep = "Refresh. If this persists, ask the administrator to check the database connection."
            });
            return [];
        }
    }

    private async Task<HashSet<string>> ReadStringsAsync(string sql, CancellationToken token)
    {
        var connection = _context.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
            await connection.OpenAsync(token);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            Add(command, "@org", _options.OrgGkey);
            Add(command, "@location", _options.LocationGkey);

            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                if (!await reader.IsDBNullAsync(0, token))
                    values.Add(reader.GetString(0));
            return values;
        }
        finally
        {
            if (closeConnection)
                await connection.CloseAsync();
        }
    }

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static FinanceSyncAttentionItemDto ToItem(FinanceSyncOutbox row,
        FinanceSyncAttention.Assessment assessment) => new()
    {
        EventId = row.Gkey,
        Kind = string.Equals(row.SourceType, "COLLECTION", StringComparison.OrdinalIgnoreCase)
            ? "Collection" : "Invoice",
        DocumentNo = row.DocumentNo ?? row.SourceEventId,
        DocumentDate = row.DocumentDate,
        Amount = Amount(row.Payload),
        State = assessment.State,
        StateLabel = assessment.StateLabel,
        Reason = assessment.Reason,
        NextStep = assessment.NextStep,
        CanSend = assessment.CanSend,
        Attempts = row.RetryCount,
        CreatedOn = row.CreatedOn,
        LastAttemptOn = row.LastAttemptOn,
        TechnicalDetail = Shorten(row.LastError, 500)
    };

    private static decimal Amount(string payload)
    {
        try
        {
            return JsonSerializer.Deserialize<FinanceDocumentPayload>(payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.DocumentAmount ?? 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static string? Shorten(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return value.Length <= length ? value : value[..length];
    }
}
