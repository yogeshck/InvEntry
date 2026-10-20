using DataAccess.Models;
using DataAccess.Services.Advances;
using InvEntry.Contracts.Schemes;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services.Schemes;

public interface ISchemeImportService
{
    /// <summary>
    /// Checks (Apply = false) or imports (Apply = true) RD schemes kept outside
    /// InvEntry. Each scheme is imported in its own transaction; schemes with
    /// errors are skipped and schemes already imported are left untouched, so the
    /// import can be run again after fixing the sheet.
    /// </summary>
    Task<SchemeImportResult> ImportAsync(
        SchemeImportRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Imports the shop's RD register. The legacy RD id becomes the account number;
/// each enrollment gets the normal monthly schedule and the payments are
/// spread over it oldest-first (as fixed-amount plan receipts are). Payments
/// are RECEIPT ledger rows without vouchers: the cash was recorded when it was
/// received, so nothing is posted to the day's cash again.
/// </summary>
public sealed class SchemeImportService : ISchemeImportService
{
    private const string ImportedBy = "RD-IMPORT";

    private readonly MijmsContext _context;
    private readonly IAuditIdentityProvider _auditIdentityProvider;

    public SchemeImportService(
        MijmsContext context,
        IAuditIdentityProvider auditIdentityProvider)
    {
        _context = context;
        _auditIdentityProvider = auditIdentityProvider;
    }

    public static string PaymentSourceRef(string legacyId, string receiptNumber) =>
        $"LEGACY-RD-{legacyId.Trim()}-{receiptNumber.Trim()}";

    public async Task<SchemeImportResult> ImportAsync(
        SchemeImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var plan = await _context.SchemePlans
                       .AsNoTracking()
                       .Include(x => x.BenefitRules)
                       .FirstOrDefaultAsync(x => x.Gkey == request.SchemePlanGkey, cancellationToken)
                   ?? throw new AdvanceAccountBusinessException("Select the scheme plan to import the RDs into.");

        if (plan.SchemeType != SchemeType.Amount || !plan.AmountFixed)
            throw new AdvanceAccountBusinessException(
                $"Plan {plan.PlanCode} must be an AMOUNT scheme with a fixed monthly amount: " +
                "imported payments are spread over the installments oldest-first.");

        var rdLedgerGkey = await _context.MtblLedgers
            .Where(x => x.LedgerAccountCode == AdvanceAccountService.RdLedgerAccountCode)
            .Select(x => (int?)x.Gkey)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AdvanceAccountBusinessException(
                $"An MTBL_LEDGERS row with account code {AdvanceAccountService.RdLedgerAccountCode} is required.");

        var flatBonus = plan.BenefitRules
            .Where(x => x.IsActive && x.BenefitType == SchemeTerms.BenefitFlatBonus)
            .Sum(x => x.BenefitValue);

        var customers = await LoadCustomersByMobileAsync(cancellationToken);

        var existingAccounts = (await _context.CustomerAdvanceAccounts
                .AsNoTracking()
                .Select(x => x.AccountNbr)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new SchemeImportResult { Applied = request.Apply, PlanCode = plan.PlanCode };

        var schemeIds = request.Schemes
            .Select(x => x.LegacyId.Trim())
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var orphan in request.Payments
                     .Where(x => !schemeIds.Contains(x.LegacyId.Trim()))
                     .GroupBy(x => x.LegacyId.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            result.GeneralIssues.Add(Error(
                $"{orphan.Count()} payment(s) for RD id '{orphan.Key}' have no row in the RD master sheet."));
        }

        var duplicateIds = request.Schemes
            .GroupBy(x => x.LegacyId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var identity = _auditIdentityProvider.GetCurrentIdentity();

        foreach (var scheme in request.Schemes)
        {
            var payments = request.Payments
                .Where(x => string.Equals(x.LegacyId.Trim(), scheme.LegacyId.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.PaidDate)
                .ThenBy(x => x.RowNumber)
                .ToList();

            var row = Prepare(
                scheme, payments, plan, flatBonus, customers, existingAccounts, duplicateIds, rdLedgerGkey, identity);

            result.Schemes.Add(row.Result);

            if (row.Result.Outcome != SchemeImportOutcome.Ready)
                continue;

            if (!request.Apply)
                continue;

            await SaveAsync(row, cancellationToken);

            row.Result.Outcome = SchemeImportOutcome.Imported;
            row.Result.AccountNbr = row.Account!.AccountNbr;
            existingAccounts.Add(row.Account.AccountNbr);
        }

        result.Imported = result.Schemes.Count(x => x.Outcome == SchemeImportOutcome.Imported);
        result.ReadyToImport = result.Schemes.Count(x => x.Outcome == SchemeImportOutcome.Ready);
        result.AlreadyImported = result.Schemes.Count(x => x.Outcome == SchemeImportOutcome.AlreadyImported);
        result.Skipped = result.Schemes.Count(x => x.Outcome == SchemeImportOutcome.Skipped);
        result.AmountImported = result.Schemes
            .Where(x => x.Outcome == SchemeImportOutcome.Imported)
            .Sum(x => x.TotalPaid);

        return result;
    }

    private sealed class PreparedScheme
    {
        public SchemeImportSchemeResult Result { get; } = new();

        public CustomerAdvanceAccount? Account { get; set; }

        public SchemeEnrollment? Enrollment { get; set; }

        public List<AdvanceLedger> Ledger { get; } = new();

        public List<SchemeInstallmentAllocation> Allocations { get; } = new();
    }

    /// <summary>
    /// Validates one scheme and builds (without saving) its account, enrollment,
    /// schedule, ledger rows and installment allocations.
    /// </summary>
    private static PreparedScheme Prepare(
        SchemeImportScheme scheme,
        IReadOnlyList<SchemeImportPayment> payments,
        SchemePlan plan,
        decimal flatBonus,
        IReadOnlyDictionary<string, List<(int Gkey, string? Name, int? TenantGkey, int? LocationGkey)>> customers,
        ISet<string> existingAccounts,
        ISet<string> duplicateIds,
        int rdLedgerGkey,
        string identity)
    {
        var prepared = new PreparedScheme();
        var result = prepared.Result;
        var legacyId = scheme.LegacyId.Trim();

        result.LegacyId = legacyId;
        result.RowNumber = scheme.RowNumber;
        result.MobileNumber = scheme.MobileNumber?.Trim();
        result.MonthlyAmount = scheme.MonthlyAmount;
        result.StartDate = scheme.StartDate;
        result.PaymentCount = payments.Count;
        result.TotalPaid = payments.Sum(x => x.Amount);

        // ---------------------------------------------------------
        // Scheme checks
        // ---------------------------------------------------------

        if (legacyId.Length == 0)
            result.Issues.Add(Error("RD id is missing."));
        else if (legacyId.Length > 50)
            result.Issues.Add(Error("RD id is longer than 50 characters."));

        if (duplicateIds.Contains(legacyId))
            result.Issues.Add(Error($"RD id {legacyId} appears more than once in the master sheet."));

        if (existingAccounts.Contains(legacyId))
        {
            result.Outcome = SchemeImportOutcome.AlreadyImported;
            result.AccountNbr = legacyId;
            return prepared;
        }

        if (scheme.TenureMonths != plan.TenureMonths)
            result.Issues.Add(Error($"Scheme duration {scheme.TenureMonths} months differs from plan {plan.PlanCode} ({plan.TenureMonths} months)."));

        if (scheme.MonthlyAmount <= 0M)
            result.Issues.Add(Error("Monthly amount must be greater than zero."));
        else if (scheme.MonthlyAmount < plan.MinInstallmentAmount)
            result.Issues.Add(Error($"Monthly amount is below the plan minimum of ₹{plan.MinInstallmentAmount:N2}."));
        else if (plan.MaxInstallmentAmount is decimal max && scheme.MonthlyAmount > max)
            result.Issues.Add(Error($"Monthly amount is above the plan maximum of ₹{max:N2}."));

        if (scheme.StartDate is not DateOnly startDate)
        {
            result.Issues.Add(Error("Start date is missing."));
            startDate = default;
        }

        // Customer: explicit key first, otherwise the mobile number.
        int? customerGkey = null;
        (int Gkey, string? Name, int? TenantGkey, int? LocationGkey)? customer = null;

        if (scheme.CustomerGkey is int explicitGkey)
        {
            var match = customers.Values.SelectMany(x => x).FirstOrDefault(x => x.Gkey == explicitGkey);
            if (match.Gkey == 0)
                result.Issues.Add(Error($"Customer {explicitGkey} was not found."));
            else
                customer = match;
        }
        else if (string.IsNullOrWhiteSpace(scheme.MobileNumber))
        {
            result.Issues.Add(Error("Mobile number is missing, so the customer cannot be found."));
        }
        else if (!customers.TryGetValue(NormaliseMobile(scheme.MobileNumber), out var matches))
        {
            result.Issues.Add(Error($"No customer has mobile number {scheme.MobileNumber!.Trim()}. Create the customer first."));
        }
        else if (matches.Count > 1)
        {
            result.Issues.Add(Error($"{matches.Count} customers have mobile number {scheme.MobileNumber!.Trim()}."));
        }
        else
        {
            customer = matches[0];
        }

        if (customer is { } found)
        {
            customerGkey = found.Gkey;
            result.CustomerGkey = found.Gkey;
            result.CustomerName = found.Name;
        }

        if (!string.IsNullOrWhiteSpace(scheme.Status) &&
            !string.Equals(scheme.Status.Trim(), "Active", StringComparison.OrdinalIgnoreCase))
        {
            result.Issues.Add(Warning($"Status in the sheet is '{scheme.Status.Trim()}'; it is imported as ACTIVE."));
        }

        var tenure = plan.TenureMonths;
        var totalDue = scheme.MonthlyAmount * tenure;
        result.TotalDue = totalDue;
        result.Outstanding = Math.Max(0M, totalDue - result.TotalPaid);

        if (startDate != default)
        {
            var maturity = SchemeTerms.MaturityDate(startDate, tenure);
            result.MaturityDate = maturity;

            if (scheme.MaturityDate is DateOnly sheetMaturity && sheetMaturity != maturity)
                result.Issues.Add(Warning($"Maturity date in the sheet {sheetMaturity:dd-MM-yyyy} differs from start + {tenure} months ({maturity:dd-MM-yyyy})."));

            if (scheme.MaturityAmount is decimal sheetAmount && sheetAmount != totalDue + flatBonus)
                result.Issues.Add(Warning($"Maturity amount in the sheet ₹{sheetAmount:N2} differs from {tenure} x ₹{scheme.MonthlyAmount:N2} + bonus ₹{flatBonus:N2} = ₹{totalDue + flatBonus:N2}."));

            if (scheme.BonusAmount is decimal sheetBonus && sheetBonus != flatBonus)
                result.Issues.Add(Warning($"Bonus in the sheet ₹{sheetBonus:N2} differs from the plan's flat bonus ₹{flatBonus:N2}."));
        }

        // ---------------------------------------------------------
        // Payment checks
        // ---------------------------------------------------------

        foreach (var payment in payments)
        {
            var where = $"Payment {payment.ReceiptNumber?.Trim() ?? $"row {payment.RowNumber}"}";

            if (payment.Amount <= 0M || decimal.Round(payment.Amount, 2) != payment.Amount)
                result.Issues.Add(Error($"{where}: amount must be greater than zero with at most two decimals."));

            if (payment.PaidDate is null)
                result.Issues.Add(Error($"{where}: paid date is missing."));
            else if (startDate != default && payment.PaidDate < startDate)
                result.Issues.Add(Warning($"{where}: paid on {payment.PaidDate:dd-MM-yyyy}, before the start date."));
            else if (payment.PaidDate > DateOnly.FromDateTime(DateTime.Today))
                result.Issues.Add(Error($"{where}: paid date {payment.PaidDate:dd-MM-yyyy} is in the future."));

            if (string.IsNullOrWhiteSpace(payment.ReceiptNumber))
                result.Issues.Add(Error($"{where}: receipt number is missing."));

            if (!string.IsNullOrWhiteSpace(payment.MobileNumber) &&
                !string.IsNullOrWhiteSpace(scheme.MobileNumber) &&
                NormaliseMobile(payment.MobileNumber) != NormaliseMobile(scheme.MobileNumber))
            {
                result.Issues.Add(Warning($"{where}: mobile {payment.MobileNumber.Trim()} differs from the scheme's {scheme.MobileNumber.Trim()}."));
            }

            if (!string.IsNullOrWhiteSpace(payment.TransactionId) &&
                payment.TransactionId.Contains("E+", StringComparison.OrdinalIgnoreCase))
            {
                result.Issues.Add(Warning($"{where}: transaction id '{payment.TransactionId.Trim()}' was shortened by Excel; the real UPI reference is lost."));
            }
        }

        foreach (var duplicate in payments
                     .Where(x => !string.IsNullOrWhiteSpace(x.ReceiptNumber))
                     .GroupBy(x => x.ReceiptNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
                     .Where(x => x.Count() > 1))
        {
            result.Issues.Add(Error($"Receipt number {duplicate.Key} is used {duplicate.Count()} times."));
        }

        if (result.TotalPaid > totalDue && totalDue > 0M)
            result.Issues.Add(Error($"₹{result.TotalPaid:N2} paid is more than the ₹{totalDue:N2} due on the whole scheme."));

        if (startDate != default && result.MaturityDate is DateOnly maturityDate &&
            payments.Any(x => x.PaidDate > maturityDate))
        {
            result.Issues.Add(Warning("Payments were received after the maturity date (accepted until the full amount is paid)."));
        }

        if (result.Issues.Any(x => x.Severity == SchemeImportSeverity.Error))
        {
            result.Outcome = SchemeImportOutcome.Skipped;
            return prepared;
        }

        // ---------------------------------------------------------
        // Build the account, enrollment, schedule and history
        // ---------------------------------------------------------

        var now = DateTime.Now;

        var account = new CustomerAdvanceAccount
        {
            AccountNbr = legacyId,
            AccountType = AdvanceAccountService.AccountTypeScheme,
            TenantGkey = customer!.Value.TenantGkey,
            LocationGkey = customer.Value.LocationGkey,
            CustGkey = customerGkey!.Value,
            MtblLedgersGkey = rdLedgerGkey,
            Status = AdvanceAccountService.StatusActive,
            OpenedOn = startDate,
            BalanceAmount = result.TotalPaid,
            TotalReceivedAmount = result.TotalPaid,
            IsOpeningBalance = true,
            Remarks = $"Imported from the RD register ({plan.PlanCode})",
            CreatedBy = identity,
            CreatedOn = now
        };

        var enrollment = new SchemeEnrollment
        {
            AdvanceAccount = account,
            SchemePlanGkey = plan.Gkey,
            TermsSnapshot = SchemeTerms.Snapshot(plan),
            InstallmentAmount = scheme.MonthlyAmount,
            StartDate = startDate,
            MaturityDate = SchemeTerms.MaturityDate(startDate, tenure),
            CreatedBy = identity,
            CreatedOn = now
        };

        foreach (var (seqNbr, dueDate) in SchemeTerms.Schedule(startDate, tenure))
        {
            enrollment.Installments.Add(new SchemeInstallment
            {
                SeqNbr = seqNbr,
                DueDate = dueDate,
                AmountDue = scheme.MonthlyAmount,
                Status = SchemeTerms.InstallmentPending
            });
        }

        var terms = SchemeTerms.ReadSnapshot(enrollment.TermsSnapshot);
        var schedule = enrollment.Installments.ToList();

        foreach (var payment in payments)
        {
            var paidOn = payment.PaidDate!.Value;

            var ledger = new AdvanceLedger
            {
                AdvanceAccount = account,
                EntryType = AdvanceLedgerEntryType.Receipt,
                EntryDate = paidOn,
                Amount = payment.Amount,
                Weight = 0M,
                SourceRef = PaymentSourceRef(legacyId, payment.ReceiptNumber!),
                Narration = Truncate(
                    $"Imported receipt {payment.ReceiptNumber!.Trim()} ({payment.Mode?.Trim() ?? "Cash"})" +
                    (string.IsNullOrWhiteSpace(payment.TransactionId) ? string.Empty : $" ref {payment.TransactionId.Trim()}"),
                    250),
                CreatedBy = identity,
                CreatedOn = now
            };

            prepared.Ledger.Add(ledger);

            IReadOnlyList<SchemeInstallmentAllocator.Allocation> allocations;
            try
            {
                allocations = SchemeInstallmentAllocator.Allocate(terms, schedule, payment.Amount, 0M, paidOn);
            }
            catch (AdvanceAccountBusinessException ex)
            {
                result.Issues.Add(Error($"Payment {payment.ReceiptNumber!.Trim()}: {ex.Message}"));
                result.Outcome = SchemeImportOutcome.Skipped;
                prepared.Ledger.Clear();
                prepared.Allocations.Clear();
                return prepared;
            }

            foreach (var allocation in allocations)
            {
                prepared.Allocations.Add(new SchemeInstallmentAllocation
                {
                    AdvanceLedger = ledger,
                    SchemeInstallment = allocation.Installment,
                    Amount = allocation.Amount
                });
            }
        }

        enrollment.InstallmentsPaid = schedule.Count(x => x.Status == SchemeTerms.InstallmentPaid);

        result.InstallmentsPaid = enrollment.InstallmentsPaid;
        result.AccountNbr = legacyId;
        result.Outcome = SchemeImportOutcome.Ready;

        prepared.Account = account;
        prepared.Enrollment = enrollment;

        return prepared;
    }

    private async Task SaveAsync(
        PreparedScheme prepared,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.CustomerAdvanceAccounts.Add(prepared.Account!);
            _context.SchemeEnrollments.Add(prepared.Enrollment!);
            _context.AdvanceLedgers.AddRange(prepared.Ledger);
            _context.SchemeInstallmentAllocations.AddRange(prepared.Allocations);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }

    private async Task<IReadOnlyDictionary<string, List<(int Gkey, string? Name, int? TenantGkey, int? LocationGkey)>>>
        LoadCustomersByMobileAsync(CancellationToken cancellationToken)
    {
        var customers = await _context.OrgCustomers
            .AsNoTracking()
            .Where(x => x.MobileNbr != null && x.MobileNbr != "")
            .Select(x => new { x.Gkey, x.MobileNbr, x.CustomerName, x.TenantGkey, x.LocationGkey })
            .ToListAsync(cancellationToken);

        return customers
            .GroupBy(x => NormaliseMobile(x.MobileNbr))
            .Where(x => x.Key.Length > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Select(c => (c.Gkey, c.CustomerName, c.TenantGkey, c.LocationGkey)).ToList());
    }

    /// <summary>Last 10 digits, so "+91 94006 97628" and "9400697628" match.</summary>
    public static string NormaliseMobile(string? mobile)
    {
        var digits = new string((mobile ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length > 10 ? digits[^10..] : digits;
    }

    private static SchemeImportIssue Error(string message) =>
        new() { Severity = SchemeImportSeverity.Error, Message = message };

    private static SchemeImportIssue Warning(string message) =>
        new() { Severity = SchemeImportSeverity.Warning, Message = message };

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
