namespace InvEntry.Contracts.Schemes;

/// <summary>
/// One-time import of RD schemes kept outside InvEntry (the shop's RD register):
/// one enrollment per scheme plus its payment history. Payments are imported as
/// opening balance history without vouchers, because the cash was already
/// recorded when it was received.
/// </summary>
public class SchemeImportRequest
{
    /// <summary>False: check and report only. True: import the schemes without errors.</summary>
    public bool Apply { get; set; }

    /// <summary>Plan the schemes are enrolled on (amount plan with a fixed monthly amount).</summary>
    public int SchemePlanGkey { get; set; }

    public List<SchemeImportScheme> Schemes { get; set; } = new();

    public List<SchemeImportPayment> Payments { get; set; } = new();
}

/// <summary>A row of the RD master sheet.</summary>
public class SchemeImportScheme
{
    /// <summary>The shop's RD id, e.g. MJ-RD26-002; kept as the account number.</summary>
    public string LegacyId { get; set; } = string.Empty;

    public string? MobileNumber { get; set; }

    /// <summary>Set when the customer cannot be found by mobile number.</summary>
    public int? CustomerGkey { get; set; }

    public int TenureMonths { get; set; }

    public decimal MonthlyAmount { get; set; }

    public DateOnly? StartDate { get; set; }

    /// <summary>From the sheet; only compared with the computed maturity date.</summary>
    public DateOnly? MaturityDate { get; set; }

    public decimal? BonusAmount { get; set; }

    /// <summary>From the sheet; only compared with monthly x tenure + bonus.</summary>
    public decimal? MaturityAmount { get; set; }

    public string? Status { get; set; }

    /// <summary>Row in the pasted sheet, for messages.</summary>
    public int RowNumber { get; set; }
}

/// <summary>A row of the RD payment ledger sheet.</summary>
public class SchemeImportPayment
{
    public string LegacyId { get; set; } = string.Empty;

    public string? MobileNumber { get; set; }

    public string? TransactionId { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? PaidDate { get; set; }

    public decimal Amount { get; set; }

    public string? Mode { get; set; }

    public string? ReceiptNumber { get; set; }

    public int RowNumber { get; set; }
}

public class SchemeImportResult
{
    public bool Applied { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public int Imported { get; set; }

    public int ReadyToImport { get; set; }

    public int AlreadyImported { get; set; }

    public int Skipped { get; set; }

    public decimal AmountImported { get; set; }

    public List<SchemeImportSchemeResult> Schemes { get; set; } = new();

    /// <summary>Problems not tied to one scheme, e.g. payments for an unknown RD id.</summary>
    public List<SchemeImportIssue> GeneralIssues { get; set; } = new();
}

public class SchemeImportSchemeResult
{
    public string LegacyId { get; set; } = string.Empty;

    public int RowNumber { get; set; }

    /// <summary>READY, IMPORTED, ALREADY_IMPORTED or SKIPPED.</summary>
    public string Outcome { get; set; } = string.Empty;

    public string? MobileNumber { get; set; }

    public int? CustomerGkey { get; set; }

    public string? CustomerName { get; set; }

    public decimal MonthlyAmount { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? MaturityDate { get; set; }

    public int PaymentCount { get; set; }

    public decimal TotalDue { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal Outstanding { get; set; }

    public int InstallmentsPaid { get; set; }

    public string? AccountNbr { get; set; }

    public List<SchemeImportIssue> Issues { get; set; } = new();

    /// <summary>Issues joined for a grid column.</summary>
    public string IssueText => string.Join("; ", Issues.Select(x => x.Message));
}

public class SchemeImportIssue
{
    /// <summary>ERROR (scheme is skipped) or WARNING (imported, but check it).</summary>
    public string Severity { get; set; } = SchemeImportSeverity.Warning;

    public string Message { get; set; } = string.Empty;
}

public static class SchemeImportSeverity
{
    public const string Error = "ERROR";
    public const string Warning = "WARNING";
}

public static class SchemeImportOutcome
{
    public const string Ready = "READY";
    public const string Imported = "IMPORTED";
    public const string AlreadyImported = "ALREADY_IMPORTED";
    public const string Skipped = "SKIPPED";
}
