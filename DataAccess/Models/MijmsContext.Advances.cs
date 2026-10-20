using Microsoft.EntityFrameworkCore;

namespace DataAccess.Models;

/// <summary>
/// Customer advance and scheme (RD) tables, created by
/// InvEntry/DatabaseScripts/Migrations/2026-10/002_Add_Customer_Advance_And_Schemes.sql.
/// Kept apart from the scaffolded MijmsContext.cs so a re-scaffold does not drop them.
/// </summary>
public partial class MijmsContext
{
    public virtual DbSet<CustomerAdvanceAccount> CustomerAdvanceAccounts { get; set; }

    public virtual DbSet<AdvanceLedger> AdvanceLedgers { get; set; }

    public virtual DbSet<SchemePlan> SchemePlans { get; set; }

    public virtual DbSet<SchemeBenefitRule> SchemeBenefitRules { get; set; }

    public virtual DbSet<SchemeEnrollment> SchemeEnrollments { get; set; }

    public virtual DbSet<SchemeInstallment> SchemeInstallments { get; set; }

    public virtual DbSet<SchemeInstallmentAllocation> SchemeInstallmentAllocations { get; set; }

    public virtual DbSet<AdvanceClosure> AdvanceClosures { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchemePlan>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_SCHEME_PLAN");

            entity.ToTable("SCHEME_PLAN");

            entity.HasIndex(e => new { e.TenantGkey, e.PlanCode }, "UQ_SCHEME_PLAN_CODE").IsUnique();

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
            entity.Property(e => e.PlanCode).HasMaxLength(30).IsUnicode(false).HasColumnName("PLAN_CODE");
            entity.Property(e => e.PlanName).HasMaxLength(100).IsUnicode(false).HasColumnName("PLAN_NAME");
            entity.Property(e => e.SchemeType).HasMaxLength(10).IsUnicode(false).HasColumnName("SCHEME_TYPE");
            entity.Property(e => e.Metal).HasMaxLength(20).HasColumnName("METAL");
            entity.Property(e => e.Purity).HasMaxLength(20).HasColumnName("PURITY");
            entity.Property(e => e.TenureMonths).HasColumnName("TENURE_MONTHS");
            entity.Property(e => e.AmountFixed).HasColumnName("AMOUNT_FIXED");
            entity.Property(e => e.MinInstallmentAmount).HasColumnType("decimal(18, 2)").HasColumnName("MIN_INSTALLMENT_AMOUNT");
            entity.Property(e => e.MaxInstallmentAmount).HasColumnType("decimal(18, 2)").HasColumnName("MAX_INSTALLMENT_AMOUNT");
            entity.Property(e => e.GraceDays).HasColumnName("GRACE_DAYS");
            entity.Property(e => e.MaxMissedForBonus).HasColumnName("MAX_MISSED_FOR_BONUS");
            entity.Property(e => e.MaxMissedBeforeLapse).HasColumnName("MAX_MISSED_BEFORE_LAPSE");
            entity.Property(e => e.MinMonthsForRefund).HasColumnName("MIN_MONTHS_FOR_REFUND");
            entity.Property(e => e.RefundDeductionPct).HasColumnType("decimal(5, 2)").HasColumnName("REFUND_DEDUCTION_PCT");
            entity.Property(e => e.RefundBasis).HasMaxLength(20).IsUnicode(false).HasColumnName("REFUND_BASIS");
            entity.Property(e => e.PreclosureBonus).HasMaxLength(10).IsUnicode(false).HasColumnName("PRECLOSURE_BONUS");
            entity.Property(e => e.ValidFrom).HasColumnName("VALID_FROM");
            entity.Property(e => e.ValidTo).HasColumnName("VALID_TO");
            entity.Property(e => e.Status).HasMaxLength(10).IsUnicode(false).HasColumnName("STATUS");
            entity.Property(e => e.TermsText).HasColumnName("TERMS_TEXT");
            entity.Property(e => e.CreatedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn).HasColumnName("CREATED_ON");
            entity.Property(e => e.ModifiedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn).HasColumnName("MODIFIED_ON");
        });

        modelBuilder.Entity<SchemeBenefitRule>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_SCHEME_BENEFIT_RULE");

            entity.ToTable("SCHEME_BENEFIT_RULE");

            entity.HasIndex(e => new { e.SchemePlanGkey, e.SeqNbr }, "UQ_SCHEME_BENEFIT_RULE_SEQ").IsUnique();

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.SchemePlanGkey).HasColumnName("SCHEME_PLAN_GKEY");
            entity.Property(e => e.SeqNbr).HasColumnName("SEQ_NBR");
            entity.Property(e => e.BenefitType).HasMaxLength(25).IsUnicode(false).HasColumnName("BENEFIT_TYPE");
            entity.Property(e => e.BenefitValue).HasColumnType("decimal(18, 3)").HasColumnName("BENEFIT_VALUE");
            entity.Property(e => e.AppliesOn).HasMaxLength(25).IsUnicode(false).HasColumnName("APPLIES_ON");
            entity.Property(e => e.MinInstallmentsPaid).HasColumnName("MIN_INSTALLMENTS_PAID");
            entity.Property(e => e.EligibleCategories).HasMaxLength(500).IsUnicode(false).HasColumnName("ELIGIBLE_CATEGORIES");
            entity.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");

            entity.HasOne(d => d.SchemePlan).WithMany(p => p.BenefitRules)
                .HasForeignKey(d => d.SchemePlanGkey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SCHEME_BENEFIT_RULE_PLAN");
        });

        modelBuilder.Entity<CustomerAdvanceAccount>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_CUSTOMER_ADVANCE_ACCOUNT");

            entity.ToTable("CUSTOMER_ADVANCE_ACCOUNT");

            entity.HasIndex(e => new { e.TenantGkey, e.AccountNbr }, "UQ_CUSTOMER_ADVANCE_ACCOUNT_NBR").IsUnique();
            entity.HasIndex(e => new { e.CustGkey, e.Status }, "IX_CUSTOMER_ADVANCE_ACCOUNT_CUSTOMER");
            entity.HasIndex(e => e.CustGkey, "UX_CUSTOMER_ADVANCE_ACCOUNT_OPEN")
                .IsUnique()
                .HasFilter("[ACCOUNT_TYPE] = 'OPEN' AND [STATUS] = 'ACTIVE'");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.TenantGkey).HasColumnName("TENANT_GKEY");
            entity.Property(e => e.LocationGkey).HasColumnName("LOCATION_GKEY");
            entity.Property(e => e.AccountNbr).HasMaxLength(50).IsUnicode(false).HasColumnName("ACCOUNT_NBR");
            entity.Property(e => e.AccountType).HasMaxLength(15).IsUnicode(false).HasColumnName("ACCOUNT_TYPE");
            entity.Property(e => e.CustGkey).HasColumnName("CUST_GKEY");
            entity.Property(e => e.MtblLedgersGkey).HasColumnName("MTBL_LEDGERS_GKEY");
            entity.Property(e => e.CustomerOrderGkey).HasColumnName("CUSTOMER_ORDER_GKEY");
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasColumnName("STATUS");
            entity.Property(e => e.OpenedOn).HasColumnName("OPENED_ON");
            entity.Property(e => e.ClosedOn).HasColumnName("CLOSED_ON");
            entity.Property(e => e.Metal).HasMaxLength(20).HasColumnName("METAL");
            entity.Property(e => e.Purity).HasMaxLength(20).HasColumnName("PURITY");
            entity.Property(e => e.BookedRate).HasColumnType("decimal(18, 2)").HasColumnName("BOOKED_RATE");
            entity.Property(e => e.ValidUntil).HasColumnName("VALID_UNTIL");
            entity.Property(e => e.BalanceAmount).HasColumnType("decimal(18, 2)").HasColumnName("BALANCE_AMOUNT");
            entity.Property(e => e.BalanceWeight).HasColumnType("decimal(18, 3)").HasColumnName("BALANCE_WEIGHT");
            entity.Property(e => e.TotalReceivedAmount).HasColumnType("decimal(18, 2)").HasColumnName("TOTAL_RECEIVED_AMOUNT");
            entity.Property(e => e.TotalReceivedWeight).HasColumnType("decimal(18, 3)").HasColumnName("TOTAL_RECEIVED_WEIGHT");
            entity.Property(e => e.IsOpeningBalance).HasColumnName("IS_OPENING_BALANCE");
            entity.Property(e => e.Remarks).HasMaxLength(250).IsUnicode(false).HasColumnName("REMARKS");
            entity.Property(e => e.CreatedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn).HasColumnName("CREATED_ON");
            entity.Property(e => e.ModifiedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn).HasColumnName("MODIFIED_ON");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("ROW_VERSION");
        });

        modelBuilder.Entity<SchemeEnrollment>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_SCHEME_ENROLLMENT");

            entity.ToTable("SCHEME_ENROLLMENT");

            entity.HasIndex(e => e.AdvanceAccountGkey, "UQ_SCHEME_ENROLLMENT_ACCOUNT").IsUnique();

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AdvanceAccountGkey).HasColumnName("ADVANCE_ACCOUNT_GKEY");
            entity.Property(e => e.SchemePlanGkey).HasColumnName("SCHEME_PLAN_GKEY");
            entity.Property(e => e.TermsSnapshot).HasColumnName("TERMS_SNAPSHOT");
            entity.Property(e => e.InstallmentAmount).HasColumnType("decimal(18, 2)").HasColumnName("INSTALLMENT_AMOUNT");
            entity.Property(e => e.StartDate).HasColumnName("START_DATE");
            entity.Property(e => e.MaturityDate).HasColumnName("MATURITY_DATE");
            entity.Property(e => e.InstallmentsPaid).HasColumnName("INSTALLMENTS_PAID");
            entity.Property(e => e.MissedCount).HasColumnName("MISSED_COUNT");
            entity.Property(e => e.BonusForfeited).HasColumnName("BONUS_FORFEITED");
            entity.Property(e => e.NomineeName).HasMaxLength(100).IsUnicode(false).HasColumnName("NOMINEE_NAME");
            entity.Property(e => e.NomineeRelation).HasMaxLength(30).IsUnicode(false).HasColumnName("NOMINEE_RELATION");
            entity.Property(e => e.NomineeMobile).HasMaxLength(15).IsUnicode(false).HasColumnName("NOMINEE_MOBILE");
            entity.Property(e => e.CreatedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn).HasColumnName("CREATED_ON");
            entity.Property(e => e.ModifiedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("MODIFIED_BY");
            entity.Property(e => e.ModifiedOn).HasColumnName("MODIFIED_ON");

            entity.HasOne(d => d.AdvanceAccount).WithOne(p => p.SchemeEnrollment)
                .HasForeignKey<SchemeEnrollment>(d => d.AdvanceAccountGkey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SCHEME_ENROLLMENT_ACCOUNT");

            entity.HasOne(d => d.SchemePlan).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.SchemePlanGkey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SCHEME_ENROLLMENT_PLAN");
        });

        modelBuilder.Entity<SchemeInstallment>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_SCHEME_INSTALLMENT");

            entity.ToTable("SCHEME_INSTALLMENT");

            entity.HasIndex(e => new { e.SchemeEnrollmentGkey, e.SeqNbr }, "UQ_SCHEME_INSTALLMENT_SEQ").IsUnique();

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.SchemeEnrollmentGkey).HasColumnName("SCHEME_ENROLLMENT_GKEY");
            entity.Property(e => e.SeqNbr).HasColumnName("SEQ_NBR");
            entity.Property(e => e.DueDate).HasColumnName("DUE_DATE");
            entity.Property(e => e.AmountDue).HasColumnType("decimal(18, 2)").HasColumnName("AMOUNT_DUE");
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18, 2)").HasColumnName("AMOUNT_PAID");
            entity.Property(e => e.WeightCredited).HasColumnType("decimal(18, 3)").HasColumnName("WEIGHT_CREDITED");
            entity.Property(e => e.Status).HasMaxLength(10).IsUnicode(false).HasColumnName("STATUS");
            entity.Property(e => e.PaidOn).HasColumnName("PAID_ON");

            entity.HasOne(d => d.SchemeEnrollment).WithMany(p => p.Installments)
                .HasForeignKey(d => d.SchemeEnrollmentGkey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SCHEME_INSTALLMENT_ENROLLMENT");
        });

        modelBuilder.Entity<AdvanceLedger>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_ADVANCE_LEDGER");

            entity.ToTable("ADVANCE_LEDGER");

            entity.HasIndex(e => e.SourceRef, "UQ_ADVANCE_LEDGER_SOURCE_REF").IsUnique();
            entity.HasIndex(e => new { e.AdvanceAccountGkey, e.EntryDate }, "IX_ADVANCE_LEDGER_ACCOUNT");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AdvanceAccountGkey).HasColumnName("ADVANCE_ACCOUNT_GKEY");
            entity.Property(e => e.EntryType).HasMaxLength(15).IsUnicode(false).HasColumnName("ENTRY_TYPE");
            entity.Property(e => e.EntryDate).HasColumnName("ENTRY_DATE");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)").HasColumnName("AMOUNT");
            entity.Property(e => e.Weight).HasColumnType("decimal(18, 3)").HasColumnName("WEIGHT");
            entity.Property(e => e.MetalRate).HasColumnType("decimal(18, 2)").HasColumnName("METAL_RATE");
            entity.Property(e => e.DailyRateGkey).HasColumnName("DAILY_RATE_GKEY");
            entity.Property(e => e.VoucherGkey).HasColumnName("VOUCHER_GKEY");
            entity.Property(e => e.InvoiceGkey).HasColumnName("INVOICE_GKEY");
            entity.Property(e => e.InvoiceNbr).HasMaxLength(50).IsUnicode(false).HasColumnName("INVOICE_NBR");
            entity.Property(e => e.AdvanceClosureGkey).HasColumnName("ADVANCE_CLOSURE_GKEY");
            entity.Property(e => e.ReversesLedgerGkey).HasColumnName("REVERSES_LEDGER_GKEY");
            entity.Property(e => e.SourceRef).HasMaxLength(100).IsUnicode(false).HasColumnName("SOURCE_REF");
            entity.Property(e => e.Narration).HasMaxLength(250).IsUnicode(false).HasColumnName("NARRATION");
            entity.Property(e => e.CreatedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("CREATED_BY");
            entity.Property(e => e.CreatedOn).HasColumnName("CREATED_ON");

            entity.HasOne(d => d.AdvanceAccount).WithMany(p => p.LedgerEntries)
                .HasForeignKey(d => d.AdvanceAccountGkey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ADVANCE_LEDGER_ACCOUNT");
        });

        modelBuilder.Entity<SchemeInstallmentAllocation>(entity =>
        {
            entity.HasKey(e => new { e.AdvanceLedgerGkey, e.SchemeInstallmentGkey })
                .HasName("PK_SCHEME_INSTALLMENT_ALLOCATION");

            entity.ToTable("SCHEME_INSTALLMENT_ALLOCATION");

            entity.Property(e => e.AdvanceLedgerGkey).HasColumnName("ADVANCE_LEDGER_GKEY");
            entity.Property(e => e.SchemeInstallmentGkey).HasColumnName("SCHEME_INSTALLMENT_GKEY");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)").HasColumnName("AMOUNT");

            entity.HasOne(d => d.AdvanceLedger).WithMany(p => p.InstallmentAllocations)
                .HasForeignKey(d => d.AdvanceLedgerGkey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SCHEME_INSTALLMENT_ALLOCATION_LEDGER");

            entity.HasOne(d => d.SchemeInstallment).WithMany(p => p.Allocations)
                .HasForeignKey(d => d.SchemeInstallmentGkey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SCHEME_INSTALLMENT_ALLOCATION_INSTALLMENT");
        });

        modelBuilder.Entity<AdvanceClosure>(entity =>
        {
            entity.HasKey(e => e.Gkey).HasName("PK_ADVANCE_CLOSURE");

            entity.ToTable("ADVANCE_CLOSURE");

            entity.Property(e => e.Gkey).HasColumnName("GKEY");
            entity.Property(e => e.AdvanceAccountGkey).HasColumnName("ADVANCE_ACCOUNT_GKEY");
            entity.Property(e => e.ClosureType).HasMaxLength(25).IsUnicode(false).HasColumnName("CLOSURE_TYPE");
            entity.Property(e => e.Status).HasMaxLength(10).IsUnicode(false).HasColumnName("STATUS");
            entity.Property(e => e.ClosureDate).HasColumnName("CLOSURE_DATE");
            entity.Property(e => e.TotalPaidAmount).HasColumnType("decimal(18, 2)").HasColumnName("TOTAL_PAID_AMOUNT");
            entity.Property(e => e.TotalPaidWeight).HasColumnType("decimal(18, 3)").HasColumnName("TOTAL_PAID_WEIGHT");
            entity.Property(e => e.MetalRate).HasColumnType("decimal(18, 2)").HasColumnName("METAL_RATE");
            entity.Property(e => e.DailyRateGkey).HasColumnName("DAILY_RATE_GKEY");
            entity.Property(e => e.MetalValue).HasColumnType("decimal(18, 2)").HasColumnName("METAL_VALUE");
            entity.Property(e => e.BonusAmount).HasColumnType("decimal(18, 2)").HasColumnName("BONUS_AMOUNT");
            entity.Property(e => e.BonusWeight).HasColumnType("decimal(18, 3)").HasColumnName("BONUS_WEIGHT");
            entity.Property(e => e.MakingWaiverPct).HasColumnType("decimal(5, 2)").HasColumnName("MAKING_WAIVER_PCT");
            entity.Property(e => e.DeductionAmount).HasColumnType("decimal(18, 2)").HasColumnName("DEDUCTION_AMOUNT");
            entity.Property(e => e.NetAmount).HasColumnType("decimal(18, 2)").HasColumnName("NET_AMOUNT");
            entity.Property(e => e.NetWeight).HasColumnType("decimal(18, 3)").HasColumnName("NET_WEIGHT");
            entity.Property(e => e.RefundMode).HasMaxLength(50).IsUnicode(false).HasColumnName("REFUND_MODE");
            entity.Property(e => e.RefundVoucherGkey).HasColumnName("REFUND_VOUCHER_GKEY");
            entity.Property(e => e.CalcBreakdown).HasColumnName("CALC_BREAKDOWN");
            entity.Property(e => e.RequestedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("REQUESTED_BY");
            entity.Property(e => e.RequestedOn).HasColumnName("REQUESTED_ON");
            entity.Property(e => e.ApprovedBy).HasMaxLength(50).IsUnicode(false).HasColumnName("APPROVED_BY");
            entity.Property(e => e.ApprovedOn).HasColumnName("APPROVED_ON");
            entity.Property(e => e.CompletedOn).HasColumnName("COMPLETED_ON");
            entity.Property(e => e.CancelReason).HasMaxLength(250).IsUnicode(false).HasColumnName("CANCEL_REASON");

            entity.HasOne(d => d.AdvanceAccount).WithMany(p => p.Closures)
                .HasForeignKey(d => d.AdvanceAccountGkey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ADVANCE_CLOSURE_ACCOUNT");
        });
    }
}
