using DataAccess.Models;
using InvEntry.Contracts.FinanceSync;

namespace DataAccess.Services;

/// <summary>
/// Turns an outbox row into what a user needs to know: is it in FinanceTracker,
/// and if not, why and what to do. Pure rules, no database access.
/// </summary>
public static class FinanceSyncAttention
{
    /// <summary>A waiting event the automatic sync has not sent within this time is stuck.</summary>
    public static readonly TimeSpan StuckAfter = TimeSpan.FromMinutes(10);

    public sealed record Assessment(string State, string StateLabel, string Reason,
        string NextStep, bool CanSend);

    /// <summary>
    /// Null when the event is in FinanceTracker and needs nothing more.
    /// </summary>
    /// <param name="nowLocal">Local time; CREATED_ON and LAST_ATTEMPT_ON are local.</param>
    /// <param name="nowUtc">UTC; CLAIM_EXPIRES_ON is UTC.</param>
    /// <param name="inSuspense">Posted, but held in FinanceTracker suspense.</param>
    public static Assessment? Assess(FinanceSyncOutbox row, DateTime nowLocal,
        DateTime nowUtc, bool inSuspense)
    {
        if (row.Status == FinanceSyncService.Sent)
        {
            return inSuspense
                ? new(FinanceSyncAttentionStates.InSuspense, "In Finance, on hold",
                    "FinanceTracker received it after its business day was closed, so it is held in Suspense and is not in that day's report or cash.",
                    $"In FinanceTracker, reopen the closed days from {DateText(row)} onwards, post it from the Suspense screen, then close the days again.",
                    false)
                : null;
        }

        if (row.ClaimExpiresOn > nowUtc)
            return new(FinanceSyncAttentionStates.InProgress, "Sending now",
                "It is being sent to FinanceTracker at this moment.",
                "Nothing to do. Refresh in a minute.", false);

        var waitingSince = row.LastAttemptOn ?? row.CreatedOn;
        var waited = nowLocal - waitingSince;

        switch (row.Status)
        {
            case FinanceSyncService.Pending when string.IsNullOrWhiteSpace(row.LastError):
                return waited < StuckAfter
                    ? new(FinanceSyncAttentionStates.InProgress, "Waiting",
                        "Queued for FinanceTracker; the automatic sync normally sends it within a minute.",
                        "Nothing to do. Refresh in a minute.", false)
                    : new(FinanceSyncAttentionStates.Stuck, "Stuck",
                        $"Not sent for {Duration(waited)}: the automatic sync has not picked it up.",
                        "Click Send. If it stays stuck, tell the administrator that the InvEntry sync service may be stopped.",
                        true);

            case FinanceSyncService.Pending:
            case FinanceSyncService.RetryableError:
            {
                var error = Explain(row.LastError);
                return error.NeedsAdmin
                    ? new(FinanceSyncAttentionStates.NeedsAdmin, "Needs administrator",
                        error.Reason, error.NextStep, true)
                    : new(FinanceSyncAttentionStates.Stuck, "Failed, retrying",
                        error.Reason,
                        $"{error.NextStep} It is also retried automatically every {FinanceSyncService.AutomaticRetryInterval.TotalMinutes:0} minutes.",
                        true);
            }

            case FinanceSyncService.Uncertain:
                return new(FinanceSyncAttentionStates.NeedsAction, "Not confirmed",
                    "FinanceTracker did not confirm whether it received this.",
                    "Click Send: it checks FinanceTracker first and never posts twice.", true);

            case FinanceSyncService.BusinessError:
            {
                var error = Explain(row.LastError);
                return new(error.NeedsAdmin ? FinanceSyncAttentionStates.NeedsAdmin : FinanceSyncAttentionStates.NeedsAction,
                    error.NeedsAdmin ? "Needs administrator" : "Needs correction",
                    error.Reason, error.NextStep, false);
            }

            default:
                return new(FinanceSyncAttentionStates.NeedsAdmin, "Needs administrator",
                    "This entry has an error from an earlier version of the sync.",
                    "Ask the administrator to review it on the FinanceTracker synchronization screen.", false);
        }
    }

    public sealed record ErrorExplanation(string Reason, string NextStep, bool NeedsAdmin);

    /// <summary>Plain reading of a stored synchronization error.</summary>
    public static ErrorExplanation Explain(string? error)
    {
        var text = error ?? string.Empty;
        bool Has(string value) => text.Contains(value, StringComparison.OrdinalIgnoreCase);

        if (Has("mapping is incomplete"))
            return new("FinanceTracker setup is missing a category or payment mode that this entry needs.",
                "Ask the administrator to run the FinanceTracker setup check, then click Send.", true);
        if (Has("CHECK constraint") || Has("could not be saved"))
            return new("The InvEntry database is out of date, so the sync result could not be saved.",
                "Ask the administrator to apply the latest InvEntry database update, then click Send.", true);
        if (Has("not finalised"))
            return new("The invoice is not finalised.",
                "Finalise the invoice in InvEntry, then click Send.", false);
        if (Has("Unable to acquire"))
            return new("FinanceTracker was busy with another entry.",
                "Click Send again in a minute.", false);
        if (Has("No eligible invoice movements") || Has("No receipt, refund, credit or old-metal"))
            return new("Nothing on this invoice can be posted to FinanceTracker: no receipt, credit, old metal or adjustment was found.",
                "Check the invoice's receipts in InvEntry. If they are correct, contact the administrator.", false);
        if (Has("not a collection or write-off"))
            return new("This receipt's type or mode is not one FinanceTracker can post.",
                "Check the receipt's mode in InvEntry. If it is correct, contact the administrator.", false);
        if (Has("not found") || Has("ambiguous") || Has("does not refer to an invoice") ||
            Has("is not active") || Has("Invalid organization"))
            return new("FinanceTracker could not match this entry to its invoice or branch.",
                "Contact the administrator.", true);
        if (Has("timeout") || Has("timed out") || Has("transport") || Has("network") ||
            Has("connection") || Has("unavailable") || Has("could not be reached"))
            return new("FinanceTracker could not be reached.",
                "Click Send again. If it keeps failing, contact the administrator.", false);

        return new("Sending to FinanceTracker failed.",
            "Click Send to try again. If it keeps failing, contact the administrator.", false);
    }

    /// <summary>Most urgent first: administrator, user action, stuck, suspense, then in progress.</summary>
    public static int Priority(string state) => state switch
    {
        FinanceSyncAttentionStates.NeedsAdmin => 0,
        FinanceSyncAttentionStates.NeedsAction => 1,
        FinanceSyncAttentionStates.Stuck => 2,
        FinanceSyncAttentionStates.InSuspense => 3,
        _ => 4
    };

    private static string DateText(FinanceSyncOutbox row) =>
        row.DocumentDate?.ToString("dd-MM-yyyy") ?? "its date";

    private static string Duration(TimeSpan value) =>
        value.TotalDays >= 1 ? $"{(int)value.TotalDays} day(s)"
        : value.TotalHours >= 1 ? $"{(int)value.TotalHours} hour(s)"
        : $"{Math.Max(1, (int)value.TotalMinutes)} minute(s)";
}
