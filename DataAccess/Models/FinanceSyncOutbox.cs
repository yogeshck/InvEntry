using System;
using System.Collections.Generic;

namespace DataAccess.Models;

public partial class FinanceSyncOutbox
{
    public long Gkey { get; set; }

    public string EventType { get; set; } = null!;

    public string SourceType { get; set; } = null!;

    public int SourceGkey { get; set; }

    public string SourceEventId { get; set; } = null!;

    public string? DocumentNo { get; set; }

    public DateTime? DocumentDate { get; set; }

    public string Payload { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int RetryCount { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? LastAttemptOn { get; set; }

    public DateTime? SentOn { get; set; }

    public string? LastError { get; set; }
}
