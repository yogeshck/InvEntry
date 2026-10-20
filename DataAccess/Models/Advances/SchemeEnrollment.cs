namespace DataAccess.Models;

public partial class SchemeEnrollment
{
    public int Gkey { get; set; }

    public int AdvanceAccountGkey { get; set; }

    public int SchemePlanGkey { get; set; }

    /// <summary>JSON copy of the plan and its benefit rules at enrollment.</summary>
    public string TermsSnapshot { get; set; } = null!;

    public decimal InstallmentAmount { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly MaturityDate { get; set; }

    public int InstallmentsPaid { get; set; }

    public int MissedCount { get; set; }

    public bool BonusForfeited { get; set; }

    public string? NomineeName { get; set; }

    public string? NomineeRelation { get; set; }

    public string? NomineeMobile { get; set; }

    /// <summary>Client id of the enrollment request; a retry returns the original.</summary>
    public Guid? ClientRequestId { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public virtual CustomerAdvanceAccount AdvanceAccount { get; set; } = null!;

    public virtual SchemePlan SchemePlan { get; set; } = null!;

    public virtual ICollection<SchemeInstallment> Installments { get; set; } = new List<SchemeInstallment>();
}

public partial class SchemeInstallment
{
    public int Gkey { get; set; }

    public int SchemeEnrollmentGkey { get; set; }

    public int SeqNbr { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal AmountDue { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal WeightCredited { get; set; }

    /// <summary>PENDING, PARTIAL, PAID, OVERDUE or WAIVED.</summary>
    public string Status { get; set; } = null!;

    public DateOnly? PaidOn { get; set; }

    public virtual SchemeEnrollment SchemeEnrollment { get; set; } = null!;

    public virtual ICollection<SchemeInstallmentAllocation> Allocations { get; set; } =
        new List<SchemeInstallmentAllocation>();
}

public partial class SchemeInstallmentAllocation
{
    public long AdvanceLedgerGkey { get; set; }

    public int SchemeInstallmentGkey { get; set; }

    public decimal Amount { get; set; }

    public virtual AdvanceLedger AdvanceLedger { get; set; } = null!;

    public virtual SchemeInstallment SchemeInstallment { get; set; } = null!;
}
