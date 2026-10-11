using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.Common;

namespace DataAccess.Services;

public interface IFinanceSyncSchemaReadinessChecker
{
    Task<IReadOnlyList<string>> FindMissingObjectsAsync(
        CancellationToken cancellationToken = default);
}

public sealed class FinanceSyncSchemaReadinessChecker
    : IFinanceSyncSchemaReadinessChecker
{
    private static readonly string[] RequiredColumns =
    [
        "GKEY", "EVENT_TYPE", "SOURCE_TYPE", "SOURCE_GKEY",
        "SOURCE_EVENT_ID", "DOCUMENT_NO", "DOCUMENT_DATE", "PAYLOAD",
        "STATUS", "RETRY_COUNT", "CREATED_ON", "LAST_ATTEMPT_ON",
        "SENT_ON", "LAST_ERROR", "CLAIM_TOKEN", "CLAIM_EXPIRES_ON"
    ];

    private readonly MijmsContext _context;

    public FinanceSyncSchemaReadinessChecker(MijmsContext context) =>
        _context = context;

    public async Task<IReadOnlyList<string>> FindMissingObjectsAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync(cancellationToken);

        try
        {
            var missing = new List<string>();
            if (!await ExistsAsync(connection,
                    "SELECT CASE WHEN OBJECT_ID('dbo.FINANCE_SYNC_OUTBOX', 'U') IS NULL THEN 0 ELSE 1 END",
                    cancellationToken))
                return ["table dbo.FINANCE_SYNC_OUTBOX"];

            foreach (var column in RequiredColumns)
            {
                if (!await ExistsAsync(connection,
                        $"SELECT CASE WHEN COL_LENGTH('dbo.FINANCE_SYNC_OUTBOX', '{column}') IS NULL THEN 0 ELSE 1 END",
                        cancellationToken))
                    missing.Add($"column dbo.FINANCE_SYNC_OUTBOX.{column}");
            }

            foreach (var index in new[]
                     {
                         "UX_FINANCE_SYNC_OUTBOX_SOURCE_EVENT",
                         "IX_FINANCE_SYNC_OUTBOX_DISPATCH_CLAIM"
                     })
            {
                using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE object_id = OBJECT_ID('dbo.FINANCE_SYNC_OUTBOX')
                          AND name = @name) THEN 1 ELSE 0 END
                    """;
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@name";
                parameter.DbType = DbType.String;
                parameter.Value = index;
                command.Parameters.Add(parameter);
                if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) != 1)
                    missing.Add($"index dbo.FINANCE_SYNC_OUTBOX.{index}");
            }

            return missing;
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }

    private static async Task<bool> ExistsAsync(
        DbConnection connection, string sql, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }
}

public sealed class FinanceSyncSchemaReadinessService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<FinanceSyncOptions> _options;
    private readonly ILogger<FinanceSyncSchemaReadinessService> _logger;

    public FinanceSyncSchemaReadinessService(
        IServiceScopeFactory scopeFactory,
        IOptions<FinanceSyncOptions> options,
        ILogger<FinanceSyncSchemaReadinessService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.Enabled) return;
        if (_options.Value.OrgGkey <= 0 || _options.Value.LocationGkey <= 0)
            throw new InvalidOperationException(
                "FinanceTracker synchronization requires positive OrgGkey and LocationGkey settings.");

        using var scope = _scopeFactory.CreateScope();
        var checker = scope.ServiceProvider
            .GetRequiredService<IFinanceSyncSchemaReadinessChecker>();
        var missing = await checker.FindMissingObjectsAsync(cancellationToken);
        if (missing.Count == 0)
        {
            _logger.LogInformation("Finance synchronization schema is ready.");
            return;
        }

        var detail = string.Join(", ", missing);
        _logger.LogCritical(
            "Finance synchronization schema is incompatible. Missing: {MissingObjects}. " +
            "Apply the reviewed migration before starting DataAccess.", detail);
        throw new InvalidOperationException(
            "Finance synchronization schema is incompatible. Missing: " + detail);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
