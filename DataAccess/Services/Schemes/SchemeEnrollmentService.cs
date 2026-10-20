using DataAccess.Models;
using DataAccess.Services.Advances;
using InvEntry.Contracts.Schemes;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services.Schemes;

public interface ISchemeEnrollmentService
{
    /// <summary>
    /// Opens the customer's SCHEME account, snapshots the plan and creates the
    /// installment schedule in one transaction. A retry with the same
    /// ClientRequestId returns the original enrollment.
    /// </summary>
    Task<SchemeEnrollmentDto> EnrollAsync(
        SchemeEnrollmentRequest request,
        CancellationToken cancellationToken = default);

    Task<SchemeEnrollmentDto?> GetEnrollmentAsync(
        int enrollmentGkey,
        CancellationToken cancellationToken = default);

    /// <summary>The customer's enrollments, newest first; closed ones only when asked.</summary>
    Task<IReadOnlyList<SchemeEnrollmentDto>> GetCustomerEnrollmentsAsync(
        int customerGkey,
        bool includeClosed,
        CancellationToken cancellationToken = default);
}

public sealed class SchemeEnrollmentService : ISchemeEnrollmentService
{
    private readonly MijmsContext _context;
    private readonly IAuditIdentityProvider _auditIdentityProvider;

    public SchemeEnrollmentService(
        MijmsContext context,
        IAuditIdentityProvider auditIdentityProvider)
    {
        _context = context;
        _auditIdentityProvider = auditIdentityProvider;
    }

    public async Task<SchemeEnrollmentDto> EnrollAsync(
        SchemeEnrollmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ClientRequestId == Guid.Empty)
            throw new AdvanceAccountBusinessException("ClientRequestId is required.");

        var replay = await FindReplayAsync(request.ClientRequestId, cancellationToken);
        if (replay is not null)
            return replay;

        var customer = await _context.OrgCustomers
                           .AsNoTracking()
                           .FirstOrDefaultAsync(x => x.Gkey == request.CustomerGkey, cancellationToken)
                       ?? throw new AdvanceAccountBusinessException("Select the customer to enroll.");

        var plan = await _context.SchemePlans
                       .AsNoTracking()
                       .Include(x => x.BenefitRules)
                       .FirstOrDefaultAsync(x => x.Gkey == request.SchemePlanGkey, cancellationToken)
                   ?? throw new AdvanceAccountBusinessException("Select the scheme plan.");

        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.Today);

        ValidateAgainstPlan(plan, request.InstallmentAmount, startDate);

        var rdLedgers = await _context.MtblLedgers
            .AsNoTracking()
            .Where(x => x.LedgerAccountCode == AdvanceAccountService.RdLedgerAccountCode)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (rdLedgers.Count != 1)
            throw new AdvanceAccountBusinessException(
                $"Exactly one MTBL_LEDGERS row with account code {AdvanceAccountService.RdLedgerAccountCode} is required.");

        var identity = _auditIdentityProvider.GetCurrentIdentity();
        var now = DateTime.Now;
        var isWeight = plan.SchemeType == SchemeType.Weight;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var account = new CustomerAdvanceAccount
            {
                // The number needs the identity: a temporary unique value is replaced after insert.
                AccountNbr = $"TMP-{Guid.NewGuid():N}",
                AccountType = AdvanceAccountService.AccountTypeScheme,
                TenantGkey = customer.TenantGkey,
                LocationGkey = customer.LocationGkey,
                CustGkey = customer.Gkey,
                MtblLedgersGkey = rdLedgers[0].Gkey,
                Status = AdvanceAccountService.StatusActive,
                OpenedOn = startDate,
                Metal = isWeight ? plan.Metal : null,
                Purity = isWeight ? plan.Purity : null,
                Remarks = Truncate($"{plan.PlanCode} {plan.PlanName}", 250),
                CreatedBy = identity,
                CreatedOn = now
            };

            _context.CustomerAdvanceAccounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);

            account.AccountNbr = $"RD-{account.Gkey:D6}";

            var enrollment = new SchemeEnrollment
            {
                AdvanceAccountGkey = account.Gkey,
                SchemePlanGkey = plan.Gkey,
                TermsSnapshot = SchemeTerms.Snapshot(plan),
                InstallmentAmount = request.InstallmentAmount,
                StartDate = startDate,
                MaturityDate = SchemeTerms.MaturityDate(startDate, plan.TenureMonths),
                NomineeName = Truncate(request.NomineeName?.Trim(), 100),
                NomineeRelation = Truncate(request.NomineeRelation?.Trim(), 30),
                NomineeMobile = Truncate(request.NomineeMobile?.Trim(), 15),
                ClientRequestId = request.ClientRequestId,
                CreatedBy = identity,
                CreatedOn = now
            };

            foreach (var (seqNbr, dueDate) in SchemeTerms.Schedule(startDate, plan.TenureMonths))
            {
                enrollment.Installments.Add(new SchemeInstallment
                {
                    SeqNbr = seqNbr,
                    DueDate = dueDate,
                    AmountDue = request.InstallmentAmount,
                    Status = SchemeTerms.InstallmentPending
                });
            }

            _context.SchemeEnrollments.Add(enrollment);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _context.ChangeTracker.Clear();

            return (await GetEnrollmentAsync(enrollment.Gkey, cancellationToken))!;
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _context.ChangeTracker.Clear();

            // A concurrent retry of the same request inserted the CLIENT_REQUEST_ID first.
            var concurrent = await FindReplayAsync(request.ClientRequestId, cancellationToken);
            if (concurrent is not null)
                return concurrent;

            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<SchemeEnrollmentDto?> GetEnrollmentAsync(
        int enrollmentGkey,
        CancellationToken cancellationToken = default)
    {
        var list = await LoadAsync(
            _context.SchemeEnrollments.Where(x => x.Gkey == enrollmentGkey),
            cancellationToken);

        return list.SingleOrDefault();
    }

    public async Task<IReadOnlyList<SchemeEnrollmentDto>> GetCustomerEnrollmentsAsync(
        int customerGkey,
        bool includeClosed,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SchemeEnrollments
            .Where(x => x.AdvanceAccount.CustGkey == customerGkey);

        if (!includeClosed)
        {
            query = query.Where(x => x.AdvanceAccount.Status != "CLOSED");
        }

        return await LoadAsync(query, cancellationToken);
    }

    /// <summary>
    /// Checks the chosen amount and start date against the plan. Shared with the
    /// receipt path so both apply the same plan rules.
    /// </summary>
    internal static void ValidateAgainstPlan(
        SchemePlan plan,
        decimal installmentAmount,
        DateOnly startDate)
    {
        if (plan.Status != InvEntry.Contracts.Schemes.SchemePlanStatus.Active)
            throw new AdvanceAccountBusinessException($"Plan {plan.PlanCode} is {plan.Status}; only ACTIVE plans take enrollments.");

        if (startDate < plan.ValidFrom || (plan.ValidTo is DateOnly validTo && startDate > validTo))
            throw new AdvanceAccountBusinessException(
                $"Plan {plan.PlanCode} takes enrollments from {plan.ValidFrom:dd-MM-yyyy}" +
                (plan.ValidTo is DateOnly to ? $" to {to:dd-MM-yyyy}." : "."));

        if (installmentAmount <= 0M || decimal.Round(installmentAmount, 2) != installmentAmount)
            throw new AdvanceAccountBusinessException("Enter the monthly installment amount (at most two decimals).");

        if (installmentAmount < plan.MinInstallmentAmount)
            throw new AdvanceAccountBusinessException(
                $"The monthly installment for {plan.PlanCode} must be at least ₹{plan.MinInstallmentAmount:N2}.");

        if (plan.MaxInstallmentAmount is decimal max && installmentAmount > max)
            throw new AdvanceAccountBusinessException(
                $"The monthly installment for {plan.PlanCode} cannot exceed ₹{max:N2}.");
    }

    private async Task<SchemeEnrollmentDto?> FindReplayAsync(
        Guid clientRequestId,
        CancellationToken cancellationToken)
    {
        var list = await LoadAsync(
            _context.SchemeEnrollments.Where(x => x.ClientRequestId == clientRequestId),
            cancellationToken);

        var replay = list.SingleOrDefault();
        if (replay is not null)
            replay.IsReplay = true;

        return replay;
    }

    private async Task<List<SchemeEnrollmentDto>> LoadAsync(
        IQueryable<SchemeEnrollment> query,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .AsNoTracking()
            .Include(x => x.AdvanceAccount)
            .Include(x => x.Installments)
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.Gkey)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return new List<SchemeEnrollmentDto>();

        var customerKeys = rows.Select(x => x.AdvanceAccount.CustGkey).Distinct().ToList();
        var customers = await _context.OrgCustomers
            .AsNoTracking()
            .Where(x => customerKeys.Contains(x.Gkey))
            .Select(x => new { x.Gkey, x.CustomerName, x.MobileNbr })
            .ToDictionaryAsync(x => x.Gkey, cancellationToken);

        return rows.Select(x =>
        {
            var terms = SchemeTerms.ReadSnapshot(x.TermsSnapshot);
            var installments = x.Installments
                .OrderBy(i => i.SeqNbr)
                .Select(i => new SchemeInstallmentDto
                {
                    Gkey = i.Gkey,
                    SeqNbr = i.SeqNbr,
                    DueDate = i.DueDate,
                    AmountDue = i.AmountDue,
                    AmountPaid = i.AmountPaid,
                    WeightCredited = i.WeightCredited,
                    Status = i.Status,
                    PaidOn = i.PaidOn
                })
                .ToList();

            customers.TryGetValue(x.AdvanceAccount.CustGkey, out var customer);

            return new SchemeEnrollmentDto
            {
                EnrollmentGkey = x.Gkey,
                AdvanceAccountGkey = x.AdvanceAccountGkey,
                AccountNbr = x.AdvanceAccount.AccountNbr,
                CustomerGkey = x.AdvanceAccount.CustGkey,
                CustomerName = customer?.CustomerName,
                CustomerMobile = customer?.MobileNbr,
                SchemePlanGkey = x.SchemePlanGkey,
                PlanCode = terms.PlanCode,
                PlanName = terms.PlanName,
                SchemeType = terms.SchemeType,
                Metal = terms.Metal,
                Purity = terms.Purity,
                AmountFixed = terms.AmountFixed,
                Status = x.AdvanceAccount.Status,
                InstallmentAmount = x.InstallmentAmount,
                StartDate = x.StartDate,
                MaturityDate = x.MaturityDate,
                TenureMonths = terms.TenureMonths,
                InstallmentsPaid = x.InstallmentsPaid,
                MissedCount = x.MissedCount,
                TotalDue = installments.Where(i => i.Status != SchemeTerms.InstallmentWaived).Sum(i => i.AmountDue),
                TotalPaid = installments.Sum(i => i.AmountPaid),
                BalanceAmount = x.AdvanceAccount.BalanceAmount,
                BalanceWeight = x.AdvanceAccount.BalanceWeight,
                NextInstallment = installments.FirstOrDefault(i =>
                    i.Status != SchemeTerms.InstallmentPaid && i.Status != SchemeTerms.InstallmentWaived),
                NomineeName = x.NomineeName,
                NomineeRelation = x.NomineeRelation,
                NomineeMobile = x.NomineeMobile,
                Installments = installments
            };
        }).ToList();
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
