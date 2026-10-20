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

    /// <summary>
    /// The enrollment whose account number is the given RD number (e.g. MJ-RD26-002
    /// or RD-000012), ignoring case and surrounding spaces; null when there is none.
    /// </summary>
    Task<SchemeEnrollmentDto?> FindByAccountNumberAsync(
        string accountNbr,
        CancellationToken cancellationToken = default);

    /// <summary>RD passbook / statement: schedule, every payment and the bonus position.</summary>
    Task<RdStatementDto?> GetStatementAsync(
        int enrollmentGkey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// RD dues and collections for a month: installments due in the month, installments
    /// paid by receipts dated in the month and, with <paramref name="includeArrears"/>,
    /// unpaid installments due earlier. Closed accounts appear only for money collected.
    /// </summary>
    Task<RdDueReportDto> GetMonthlyDuesAsync(
        int year,
        int month,
        bool includeArrears,
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

    public async Task<SchemeEnrollmentDto?> FindByAccountNumberAsync(
        string accountNbr,
        CancellationToken cancellationToken = default)
    {
        var number = accountNbr?.Trim();

        if (string.IsNullOrEmpty(number))
            return null;

        // Staff may type "mj-rd26-002"; account numbers are unique, so compare without case.
        var upper = number.ToUpperInvariant();

        var list = await LoadAsync(
            _context.SchemeEnrollments.Where(x => x.AdvanceAccount.AccountNbr.ToUpper() == upper),
            cancellationToken);

        return list.FirstOrDefault();
    }

    public async Task<RdStatementDto?> GetStatementAsync(
        int enrollmentGkey,
        CancellationToken cancellationToken = default)
    {
        var scheme = await GetEnrollmentAsync(enrollmentGkey, cancellationToken);
        if (scheme is null)
            return null;

        var termsSnapshot = await _context.SchemeEnrollments
            .AsNoTracking()
            .Where(x => x.Gkey == enrollmentGkey)
            .Select(x => x.TermsSnapshot)
            .SingleAsync(cancellationToken);

        var terms = SchemeTerms.ReadSnapshot(termsSnapshot);

        var entries = await (
                from l in _context.AdvanceLedgers.AsNoTracking()
                join v in _context.Vouchers.AsNoTracking() on l.VoucherGkey equals (int?)v.Gkey into vj
                from v in vj.DefaultIfEmpty()
                where l.AdvanceAccountGkey == scheme.AdvanceAccountGkey
                orderby l.EntryDate, l.Gkey
                select new
                {
                    l.Gkey,
                    l.EntryDate,
                    l.EntryType,
                    l.Amount,
                    l.Weight,
                    l.MetalRate,
                    l.SourceRef,
                    l.Narration,
                    l.VoucherGkey,
                    VoucherNbr = v.VoucherNbr,
                    VoucherMode = v.Mode
                })
            .ToListAsync(cancellationToken);

        var installmentBySeq = scheme.Installments.ToDictionary(x => x.Gkey, x => x);

        var allocations = await _context.SchemeInstallmentAllocations
            .AsNoTracking()
            .Where(x => x.SchemeInstallment.SchemeEnrollmentGkey == enrollmentGkey)
            .Select(x => new { x.AdvanceLedgerGkey, x.SchemeInstallmentGkey, x.Amount })
            .ToListAsync(cancellationToken);

        var legacyPrefix = $"LEGACY-RD-{scheme.AccountNbr}-";
        var paidSoFar = new Dictionary<int, decimal>();
        var balance = 0M;
        var payments = new List<RdStatementPayment>();

        foreach (var entry in entries)
        {
            balance += entry.Amount;

            // Installments this entry paid, judged with the payments up to it.
            var covered = new List<string>();
            foreach (var allocation in allocations.Where(x => x.AdvanceLedgerGkey == entry.Gkey))
            {
                paidSoFar[allocation.SchemeInstallmentGkey] =
                    paidSoFar.GetValueOrDefault(allocation.SchemeInstallmentGkey) + allocation.Amount;

                if (installmentBySeq.TryGetValue(allocation.SchemeInstallmentGkey, out var installment))
                {
                    covered.Add(paidSoFar[allocation.SchemeInstallmentGkey] >= installment.AmountDue
                        ? installment.SeqNbr.ToString()
                        : $"{installment.SeqNbr} (part)");
                }
            }

            var isImported = entry.VoucherGkey is null &&
                             entry.SourceRef.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase);

            payments.Add(new RdStatementPayment
            {
                Date = entry.EntryDate,
                ReceiptNumber = entry.VoucherNbr
                                ?? (isImported ? entry.SourceRef[legacyPrefix.Length..] : entry.EntryType),
                Mode = entry.VoucherMode ?? ModeFromNarration(entry.Narration),
                EntryType = entry.EntryType,
                Amount = entry.Amount,
                Weight = entry.Weight,
                MetalRate = entry.MetalRate,
                InstallmentsCovered = covered.Count == 0 ? null : string.Join(", ", covered),
                RunningBalance = balance,
                IsImported = isImported
            });
        }

        var bonusRules = terms.BenefitRules
            .Where(x => x.IsActive && x.BenefitType == SchemeTerms.BenefitFlatBonus)
            .ToList();

        var bonusAmount = bonusRules.Sum(x => x.BenefitValue);
        var bonusMin = bonusRules.Count == 0
            ? terms.TenureMonths
            : bonusRules.Max(x => x.MinInstallmentsPaid == 0 ? terms.TenureMonths : x.MinInstallmentsPaid);
        var bonusEarned = bonusAmount > 0M && scheme.InstallmentsPaid >= bonusMin;

        return new RdStatementDto
        {
            Scheme = scheme,
            Payments = payments,
            BonusAmount = bonusAmount,
            BonusMinInstallments = bonusMin,
            BonusEarned = bonusEarned,
            MaturityValue = scheme.TotalPaid + (bonusEarned ? bonusAmount : 0M),
            GeneratedOn = DateTime.Now
        };
    }

    /// <summary>"Imported receipt RDR-0001 (Gpay) ref ..." gives "Gpay".</summary>
    private static string? ModeFromNarration(string? narration)
    {
        if (string.IsNullOrEmpty(narration))
            return null;

        var open = narration.IndexOf('(');
        var close = open < 0 ? -1 : narration.IndexOf(')', open + 1);

        return close > open + 1 ? narration[(open + 1)..close] : null;
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

    public async Task<RdDueReportDto> GetMonthlyDuesAsync(
        int year,
        int month,
        bool includeArrears,
        CancellationToken cancellationToken = default)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            throw new AdvanceAccountBusinessException("A valid year and month are required.");

        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        var installments = await _context.SchemeInstallments
            .AsNoTracking()
            .Where(i => i.Status != SchemeTerms.InstallmentWaived)
            .Where(i =>
                (i.DueDate >= from && i.DueDate < to) ||
                (includeArrears && i.DueDate < from && i.Status != SchemeTerms.InstallmentPaid) ||
                i.Allocations.Any(a => a.AdvanceLedger.EntryDate >= from && a.AdvanceLedger.EntryDate < to))
            .Select(i => new
            {
                i.Gkey,
                i.SchemeEnrollmentGkey,
                i.SeqNbr,
                i.DueDate,
                i.AmountDue,
                i.AmountPaid,
                i.Status,
                Collected = i.Allocations
                    .Where(a => a.AdvanceLedger.EntryDate >= from && a.AdvanceLedger.EntryDate < to)
                    .Sum(a => (decimal?)a.Amount) ?? 0M,
                LastPaidOn = i.Allocations.Max(a => (DateOnly?)a.AdvanceLedger.EntryDate),
                i.SchemeEnrollment.AdvanceAccount.AccountNbr,
                AccountStatus = i.SchemeEnrollment.AdvanceAccount.Status,
                i.SchemeEnrollment.AdvanceAccount.CustGkey,
                i.SchemeEnrollment.TermsSnapshot
            })
            .ToListAsync(cancellationToken);

        // A closed scheme owes nothing more; it is listed only for money collected in the month.
        installments = installments
            .Where(x => x.AccountStatus != "CLOSED" || x.Collected != 0M)
            .ToList();

        var customerKeys = installments.Select(x => x.CustGkey).Distinct().ToList();
        var customers = await _context.OrgCustomers
            .AsNoTracking()
            .Where(x => customerKeys.Contains(x.Gkey))
            .Select(x => new { x.Gkey, x.CustomerName, x.MobileNbr })
            .ToDictionaryAsync(x => x.Gkey, cancellationToken);

        var terms = installments
            .Select(x => x.SchemeEnrollmentGkey)
            .Distinct()
            .ToDictionary(
                key => key,
                key => SchemeTerms.ReadSnapshot(installments.First(x => x.SchemeEnrollmentGkey == key).TermsSnapshot));

        var rows = installments.Select(x =>
        {
            customers.TryGetValue(x.CustGkey, out var customer);
            var plan = terms[x.SchemeEnrollmentGkey];
            var balance = Math.Max(0M, x.AmountDue - x.AmountPaid);
            var overdue = balance > 0M && x.DueDate < today;

            return new RdDueRowDto
            {
                InstallmentGkey = x.Gkey,
                EnrollmentGkey = x.SchemeEnrollmentGkey,
                AccountNbr = x.AccountNbr,
                AccountStatus = x.AccountStatus,
                CustomerGkey = x.CustGkey,
                CustomerName = customer?.CustomerName,
                CustomerMobile = customer?.MobileNbr,
                PlanName = plan.PlanName,
                SeqNbr = x.SeqNbr,
                TenureMonths = plan.TenureMonths,
                DueDate = x.DueDate,
                AmountDue = x.AmountDue,
                AmountPaid = x.AmountPaid,
                Balance = balance,
                CollectedInMonth = x.Collected,
                LastPaidOn = x.LastPaidOn,
                Status = x.Status,
                StatusText = balance <= 0M
                    ? "Paid"
                    : x.AmountPaid > 0M
                        ? overdue ? "Overdue (part paid)" : "Part paid"
                        : overdue ? "Overdue" : "Pending",
                DaysOverdue = overdue ? today.DayNumber - x.DueDate.DayNumber : 0,
                IsArrear = x.DueDate < from && balance > 0M
            };
        })
        .OrderBy(x => x.DueDate)
        .ThenBy(x => x.AccountNbr)
        .ThenBy(x => x.SeqNbr)
        .ToList();

        return new RdDueReportDto
        {
            Year = year,
            Month = month,
            IncludesArrears = includeArrears,
            GeneratedOn = now,
            Rows = rows
        };
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
