namespace InvEntry.Contracts.Schemes;

/// <summary>
/// Enrolls a customer in a scheme plan: opens the customer's SCHEME account,
/// snapshots the plan terms and creates the monthly installment schedule.
/// </summary>
public class SchemeEnrollmentRequest
{
    /// <summary>
    /// Generated once by the client per enrollment and reused on every retry;
    /// a retry returns the original enrollment.
    /// </summary>
    public Guid ClientRequestId { get; set; }

    public int CustomerGkey { get; set; }

    public int SchemePlanGkey { get; set; }

    /// <summary>Monthly amount chosen by the customer, within the plan's limits.</summary>
    public decimal InstallmentAmount { get; set; }

    /// <summary>First installment's due date. Defaults to today.</summary>
    public DateOnly? StartDate { get; set; }

    public string? NomineeName { get; set; }

    public string? NomineeRelation { get; set; }

    public string? NomineeMobile { get; set; }
}

public class SchemeEnrollmentDto
{
    public int EnrollmentGkey { get; set; }

    public int AdvanceAccountGkey { get; set; }

    public string AccountNbr { get; set; } = string.Empty;

    public int CustomerGkey { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerMobile { get; set; }

    public int SchemePlanGkey { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public string PlanName { get; set; } = string.Empty;

    public string SchemeType { get; set; } = string.Empty;

    public string? Metal { get; set; }

    public string? Purity { get; set; }

    public bool AmountFixed { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal InstallmentAmount { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly MaturityDate { get; set; }

    public int TenureMonths { get; set; }

    public int InstallmentsPaid { get; set; }

    public int MissedCount { get; set; }

    public decimal TotalDue { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal BalanceAmount { get; set; }

    public decimal BalanceWeight { get; set; }

    /// <summary>Earliest installment not fully paid; null when all are paid.</summary>
    public SchemeInstallmentDto? NextInstallment { get; set; }

    public string? NomineeName { get; set; }

    public string? NomineeRelation { get; set; }

    public string? NomineeMobile { get; set; }

    /// <summary>True when the request was a retry of an already recorded enrollment.</summary>
    public bool IsReplay { get; set; }

    public List<SchemeInstallmentDto> Installments { get; set; } = new();

    /// <summary>One-line description for pickers, e.g. "RD-000012 · Gold 11+1 · paid 3/11".</summary>
    public string DisplayText =>
        $"{AccountNbr} · {PlanName} · paid {InstallmentsPaid}/{TenureMonths}" +
        (NextInstallment is { } next ? $" · next ₹{next.AmountOutstanding:N2} due {next.DueDate:dd-MM-yyyy}" : " · all paid");
}

public class SchemeInstallmentDto
{
    public int Gkey { get; set; }

    public int SeqNbr { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal AmountDue { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal WeightCredited { get; set; }

    /// <summary>PENDING, PARTIAL, PAID, OVERDUE or WAIVED.</summary>
    public string Status { get; set; } = string.Empty;

    public DateOnly? PaidOn { get; set; }

    public decimal AmountOutstanding => Math.Max(0M, AmountDue - AmountPaid);
}
