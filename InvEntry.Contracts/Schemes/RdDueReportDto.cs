using System;
using System.Collections.Generic;

namespace InvEntry.Contracts.Schemes;

/// <summary>
/// RD installments for one month: those due in the month, those that received a
/// payment in the month (early or late) and, optionally, unpaid installments
/// from earlier months.
/// </summary>
public sealed class RdDueReportDto
{
    public int Year { get; set; }

    public int Month { get; set; }

    public bool IncludesArrears { get; set; }

    public DateTime GeneratedOn { get; set; }

    public List<RdDueRowDto> Rows { get; set; } = new();
}

public sealed class RdDueRowDto
{
    public int InstallmentGkey { get; set; }

    public int EnrollmentGkey { get; set; }

    public string AccountNbr { get; set; } = string.Empty;

    /// <summary>ACTIVE, MATURED, CLOSED ...</summary>
    public string AccountStatus { get; set; } = string.Empty;

    public int CustomerGkey { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerMobile { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public int SeqNbr { get; set; }

    public int TenureMonths { get; set; }

    /// <summary>"3 / 12".</summary>
    public string InstallmentText => $"{SeqNbr} / {TenureMonths}";

    public DateOnly DueDate { get; set; }

    public decimal AmountDue { get; set; }

    /// <summary>Paid so far against this installment, in any month.</summary>
    public decimal AmountPaid { get; set; }

    public decimal Balance { get; set; }

    /// <summary>Paid against this installment by receipts dated in the report month.</summary>
    public decimal CollectedInMonth { get; set; }

    /// <summary>Date of the latest receipt applied to this installment.</summary>
    public DateOnly? LastPaidOn { get; set; }

    /// <summary>PENDING, PARTIAL, PAID or OVERDUE (stored status).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Paid, Part paid, Pending, Overdue, Overdue (part paid).</summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>Days past the due date while a balance remains; 0 otherwise.</summary>
    public int DaysOverdue { get; set; }

    /// <summary>Due before the report month and still unpaid.</summary>
    public bool IsArrear { get; set; }

    public bool IsDue => Balance > 0M;

    public bool IsCollected => CollectedInMonth != 0M;
}
