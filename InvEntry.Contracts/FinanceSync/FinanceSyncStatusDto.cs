using System;
using System.Collections.Generic;

namespace InvEntry.Contracts.FinanceSync;

/// <summary>
/// Plain-language state of InvEntry -> FinanceTracker synchronization for users:
/// what is not in FinanceTracker yet, why, and what to do about it.
/// </summary>
public sealed class FinanceSyncStatusDto
{
    public DateTime CheckedOn { get; set; }

    /// <summary>OK, ATTENTION (something for a user to do) or PROBLEM (needs the administrator).</summary>
    public string OverallState { get; set; } = FinanceSyncOverallStates.Ok;

    public string Headline { get; set; } = string.Empty;

    /// <summary>The automatic sync has completed a cycle recently.</summary>
    public bool AutomaticSyncRunning { get; set; }

    public DateTime? LastAutomaticSyncOn { get; set; }

    /// <summary>Problems an administrator has to fix (setup, database, sync service).</summary>
    public List<FinanceSyncIssueDto> Issues { get; set; } = new();

    public int InProgressCount { get; set; }

    public int StuckCount { get; set; }

    public int NeedsActionCount { get; set; }

    public int NeedsAdminCount { get; set; }

    public int InSuspenseCount { get; set; }

    /// <summary>Events that can be sent from the status screen right now.</summary>
    public int SendableCount { get; set; }

    /// <summary>Everything not (fully) in FinanceTracker, most urgent first.</summary>
    public List<FinanceSyncAttentionItemDto> Items { get; set; } = new();
}

public sealed class FinanceSyncIssueDto
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string NextStep { get; set; } = string.Empty;
}

public sealed class FinanceSyncAttentionItemDto
{
    public long EventId { get; set; }

    /// <summary>Invoice or Collection.</summary>
    public string Kind { get; set; } = string.Empty;

    public string DocumentNo { get; set; } = string.Empty;

    public DateTime? DocumentDate { get; set; }

    public decimal Amount { get; set; }

    /// <summary>One of <see cref="FinanceSyncAttentionStates"/>.</summary>
    public string State { get; set; } = string.Empty;

    public string StateLabel { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string NextStep { get; set; } = string.Empty;

    public bool CanSend { get; set; }

    public int Attempts { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? LastAttemptOn { get; set; }

    /// <summary>The stored error, for the administrator.</summary>
    public string? TechnicalDetail { get; set; }
}

public sealed class FinanceSyncSendResultDto
{
    public int Requested { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid? JobId { get; set; }
}

public static class FinanceSyncOverallStates
{
    public const string Ok = "OK";
    public const string Attention = "ATTENTION";
    public const string Problem = "PROBLEM";
}

public static class FinanceSyncAttentionStates
{
    public const string InProgress = "IN_PROGRESS";
    public const string Stuck = "STUCK";
    public const string NeedsAction = "NEEDS_ACTION";
    public const string NeedsAdmin = "NEEDS_ADMIN";
    public const string InSuspense = "IN_SUSPENSE";
}

/// <summary>No event ids sends every entry that can be sent.</summary>
public sealed class FinanceSyncSendRequestDto
{
    public List<long>? EventIds { get; set; }

    /// <summary>Who asked, for the server log.</summary>
    public string? Actor { get; set; }
}
