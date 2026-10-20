using DataAccess.Models;
using InvEntry.Contracts.Advances;
using InvEntry.Contracts.Invoices;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services.Advances;

/// <summary>
/// Server-side handling of customer advance and RD balances.
///
/// CUSTOMER_ADVANCE_ACCOUNT.BALANCE_AMOUNT / TOTAL_RECEIVED_AMOUNT are a cache of
/// ADVANCE_LEDGER and are only changed by a conditional UPDATE in the same
/// transaction as the ledger insert. Ledger rows are never updated or deleted.
/// </summary>
public sealed class AdvanceAccountService : IAdvanceAccountService
{
    public const string AdvanceAdjMode = "Advance Adj";
    public const string RdAdjMode = "RD Adj";

    public const int AdvanceLedgerAccountCode = 1000;
    public const int RdLedgerAccountCode = 3000;

    // Ledger the money comes from on a receipt voucher (as the client used to send).
    public const int ReceiptFromLedgerAccountCode = 2000;

    public const string AccountTypeOpen = "OPEN";
    public const string AccountTypeScheme = "SCHEME";
    public const string StatusActive = "ACTIVE";

    private const string ReceiptVoucherTransType = "Receipt";

    private readonly MijmsContext _context;
    private readonly IVoucherNumberService _voucherNumberService;
    private readonly IAuditIdentityProvider _auditIdentityProvider;

    public AdvanceAccountService(
        MijmsContext context,
        IVoucherNumberService voucherNumberService,
        IAuditIdentityProvider auditIdentityProvider)
    {
        _context = context;
        _voucherNumberService = voucherNumberService;
        _auditIdentityProvider = auditIdentityProvider;
    }

    public static string ReceiptSourceRef(Guid clientRequestId) =>
        $"RECEIPT-{clientRequestId:N}";

    public static string InvoiceAdjustmentSourceRef(int invoiceGkey, int advanceAccountGkey) =>
        $"INVOICE-{invoiceGkey}-ADJ-{advanceAccountGkey}";

    public static bool IsAdjustmentMode(string? mode) =>
        string.Equals(mode?.Trim(), AdvanceAdjMode, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mode?.Trim(), RdAdjMode, StringComparison.OrdinalIgnoreCase);

    /// <summary>RD money lives in SCHEME accounts; every other account type is an advance.</summary>
    public static string SettlementModeFor(string accountType) =>
        string.Equals(accountType, AccountTypeScheme, StringComparison.OrdinalIgnoreCase)
            ? RdAdjMode
            : AdvanceAdjMode;

    // =========================================================
    // RECEIPT
    // =========================================================

    public async Task<AdvanceReceiptResponse> CreateReceiptAsync(
        AdvanceReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ClientRequestId == Guid.Empty)
            throw new AdvanceAccountBusinessException("ClientRequestId is required.");

        if (request.CustomerGkey <= 0)
            throw new AdvanceAccountBusinessException("Customer is required.");

        if (request.Amount <= 0M)
            throw new AdvanceAccountBusinessException("Receipt amount must be greater than zero.");

        if (decimal.Round(request.Amount, 2) != request.Amount)
            throw new AdvanceAccountBusinessException("Receipt amount cannot have more than two decimals.");

        if (string.IsNullOrWhiteSpace(request.Mode))
            throw new AdvanceAccountBusinessException("Receipt mode is required.");

        var receiptType = NormaliseReceiptType(request.ReceiptType);
        var sourceRef = ReceiptSourceRef(request.ClientRequestId);

        var existing = await FindReplayAsync(sourceRef, cancellationToken);
        if (existing is not null)
            return existing;

        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var customerExists = await _context.OrgCustomers
                .AnyAsync(x => x.Gkey == request.CustomerGkey, cancellationToken);

            if (!customerExists)
                throw new AdvanceAccountBusinessException(
                    $"Customer {request.CustomerGkey} was not found.");

            var isRd = receiptType == AdvanceReceiptType.RecurringDeposit;
            var toLedger = await GetSingleLedgerAsync(
                isRd ? RdLedgerAccountCode : AdvanceLedgerAccountCode, cancellationToken);

            var fromLedgerGkey = await _context.MtblLedgers
                .Where(x => x.LedgerAccountCode == ReceiptFromLedgerAccountCode)
                .Select(x => (int?)x.Gkey)
                .FirstOrDefaultAsync(cancellationToken);

            var now = DateTime.Now;
            var transDate = request.TransDate ?? now;
            var identity = _auditIdentityProvider.GetCurrentIdentity();

            var account = await ResolveReceiptAccountAsync(
                request, isRd, toLedger.Gkey, transDate, identity, cancellationToken);

            var voucher = new Voucher
            {
                SeqNbr = await NextDailySequenceAsync(transDate, cancellationToken),
                CustomerGkey = request.CustomerGkey,
                TransType = ReceiptVoucherTransType,
                VoucherType = receiptType,
                Mode = request.Mode.Trim(),
                TransAmount = request.Amount,
                VoucherNbr = await _voucherNumberService.GetNextNumberAsync(
                    receiptType, cancellationToken),
                VoucherDate = transDate,
                TransDate = transDate,
                TransDesc = Truncate(request.TransDesc, 50),
                FromLedgerGkey = fromLedgerGkey,
                ToLedgerGkey = toLedger.Gkey
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync(cancellationToken);

            var ledger = new AdvanceLedger
            {
                AdvanceAccountGkey = account.Gkey,
                EntryType = AdvanceLedgerEntryType.Receipt,
                EntryDate = DateOnly.FromDateTime(transDate),
                Amount = request.Amount,
                Weight = 0M,
                VoucherGkey = voucher.Gkey,
                SourceRef = sourceRef,
                Narration = Truncate(
                    $"{receiptType} {voucher.VoucherNbr} ({voucher.Mode})", 250),
                CreatedBy = identity,
                CreatedOn = now
            };

            _context.AdvanceLedgers.Add(ledger);

            var updated = await _context.CustomerAdvanceAccounts
                .Where(x => x.Gkey == account.Gkey && x.Status == StatusActive)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.BalanceAmount, x => x.BalanceAmount + request.Amount)
                    .SetProperty(x => x.TotalReceivedAmount, x => x.TotalReceivedAmount + request.Amount)
                    .SetProperty(x => x.ModifiedBy, identity)
                    .SetProperty(x => x.ModifiedOn, now),
                    cancellationToken);

            if (updated == 0)
                throw new AdvanceAccountBusinessException(
                    $"Advance account {account.AccountNbr} is no longer active.");

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var balance = await _context.CustomerAdvanceAccounts
                .Where(x => x.Gkey == account.Gkey)
                .Select(x => x.BalanceAmount)
                .SingleAsync(cancellationToken);

            return new AdvanceReceiptResponse
            {
                VoucherGkey = voucher.Gkey,
                VoucherNbr = voucher.VoucherNbr ?? string.Empty,
                VoucherDate = voucher.VoucherDate,
                AdvanceAccountGkey = account.Gkey,
                AccountNbr = account.AccountNbr,
                AdvanceLedgerGkey = ledger.Gkey,
                Amount = request.Amount,
                BalanceAmount = balance,
                IsReplay = false
            };
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _context.ChangeTracker.Clear();

            // A concurrent retry of the same request inserted the SOURCE_REF first.
            var replay = await FindReplayAsync(sourceRef, cancellationToken);
            if (replay is not null)
                return replay;

            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<AdvanceReceiptResponse?> FindReplayAsync(
        string sourceRef,
        CancellationToken cancellationToken)
    {
        var row = await (
                from l in _context.AdvanceLedgers.AsNoTracking()
                join a in _context.CustomerAdvanceAccounts.AsNoTracking()
                    on l.AdvanceAccountGkey equals a.Gkey
                join v in _context.Vouchers.AsNoTracking()
                    on l.VoucherGkey equals (int?)v.Gkey into vj
                from v in vj.DefaultIfEmpty()
                where l.SourceRef == sourceRef
                select new
                {
                    l.Gkey,
                    l.Amount,
                    a.AccountNbr,
                    AccountGkey = a.Gkey,
                    a.BalanceAmount,
                    VoucherGkey = (int?)v.Gkey,
                    v.VoucherNbr,
                    v.VoucherDate
                })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        return new AdvanceReceiptResponse
        {
            VoucherGkey = row.VoucherGkey ?? 0,
            VoucherNbr = row.VoucherNbr ?? string.Empty,
            VoucherDate = row.VoucherDate,
            AdvanceAccountGkey = row.AccountGkey,
            AccountNbr = row.AccountNbr,
            AdvanceLedgerGkey = row.Gkey,
            Amount = row.Amount,
            BalanceAmount = row.BalanceAmount,
            IsReplay = true
        };
    }

    private async Task<CustomerAdvanceAccount> ResolveReceiptAccountAsync(
        AdvanceReceiptRequest request,
        bool isRd,
        int ledgerGkey,
        DateTime transDate,
        string identity,
        CancellationToken cancellationToken)
    {
        if (request.AdvanceAccountGkey is int accountGkey)
        {
            var account = await _context.CustomerAdvanceAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Gkey == accountGkey, cancellationToken)
                ?? throw new AdvanceAccountBusinessException(
                    $"Advance account {accountGkey} was not found.");

            EnsureUsable(account, request.CustomerGkey, isRd ? RdAdjMode : AdvanceAdjMode);
            return account;
        }

        // Without an explicit account, an advance goes to the customer's open
        // advance account and RD money to their scheme account.
        var accountType = isRd ? AccountTypeScheme : AccountTypeOpen;

        var existing = await FindSingleActiveAccountAsync(
            request.CustomerGkey, accountType, cancellationToken);

        if (existing is not null)
            return existing;

        // First receipt of this kind for the customer: open an account. The number
        // needs the identity, so a temporary unique number is replaced after insert.
        var created = new CustomerAdvanceAccount
        {
            AccountNbr = $"TMP-{Guid.NewGuid():N}",
            AccountType = accountType,
            CustGkey = request.CustomerGkey,
            MtblLedgersGkey = ledgerGkey,
            Status = StatusActive,
            OpenedOn = DateOnly.FromDateTime(transDate),
            CreatedBy = identity,
            CreatedOn = DateTime.Now
        };

        _context.CustomerAdvanceAccounts.Add(created);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (accountType == AccountTypeOpen)
        {
            // UX_CUSTOMER_ADVANCE_ACCOUNT_OPEN: a concurrent first receipt for
            // this customer opened the OPEN account first. Only the failed
            // statement is rolled back, so the transaction can credit theirs.
            _context.Entry(created).State = EntityState.Detached;

            return await FindSingleActiveAccountAsync(
                       request.CustomerGkey, accountType, cancellationToken)
                   ?? throw new AdvanceAccountBusinessException(
                       "The customer's advance account could not be opened. Try again.");
        }

        created.AccountNbr = $"{(isRd ? "RD" : "ADV")}-{created.Gkey:D6}";
        await _context.SaveChangesAsync(cancellationToken);

        // Detach so the conditional UPDATE below is the only balance writer.
        _context.Entry(created).State = EntityState.Detached;
        return created;
    }

    private async Task<CustomerAdvanceAccount?> FindSingleActiveAccountAsync(
        int customerGkey,
        string accountType,
        CancellationToken cancellationToken)
    {
        var candidates = await _context.CustomerAdvanceAccounts
            .AsNoTracking()
            .Where(x => x.CustGkey == customerGkey &&
                        x.AccountType == accountType &&
                        x.Status == StatusActive)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (candidates.Count > 1)
            throw new AdvanceAccountBusinessException(
                $"The customer has more than one active {accountType} account. " +
                "Select the account to credit.");

        return candidates.SingleOrDefault();
    }

    private async Task<int> NextDailySequenceAsync(
        DateTime voucherDate,
        CancellationToken cancellationToken)
    {
        var start = voucherDate.Date;
        var end = start.AddDays(1);

        var last = await _context.Vouchers
            .Where(x => x.VoucherDate >= start && x.VoucherDate < end)
            .MaxAsync(x => x.SeqNbr, cancellationToken);

        return last.GetValueOrDefault() + 1;
    }

    private async Task<MtblLedger> GetSingleLedgerAsync(
        int accountCode,
        CancellationToken cancellationToken)
    {
        var ledgers = await _context.MtblLedgers
            .AsNoTracking()
            .Where(x => x.LedgerAccountCode == accountCode)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (ledgers.Count != 1)
            throw new AdvanceAccountBusinessException(
                $"Exactly one MTBL_LEDGERS row with account code {accountCode} is required.");

        return ledgers[0];
    }

    private static string NormaliseReceiptType(string? receiptType)
    {
        if (string.Equals(receiptType?.Trim(), AdvanceReceiptType.AdvanceReceipt,
                StringComparison.OrdinalIgnoreCase))
            return AdvanceReceiptType.AdvanceReceipt;

        if (string.Equals(receiptType?.Trim(), AdvanceReceiptType.RecurringDeposit,
                StringComparison.OrdinalIgnoreCase))
            return AdvanceReceiptType.RecurringDeposit;

        throw new AdvanceAccountBusinessException(
            $"Receipt type must be '{AdvanceReceiptType.AdvanceReceipt}' or " +
            $"'{AdvanceReceiptType.RecurringDeposit}'.");
    }

    // =========================================================
    // AVAILABLE ACCOUNTS
    // =========================================================

    public async Task<IReadOnlyList<AdvanceAccountSummary>> GetAvailableAccountsAsync(
        int customerGkey,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
                from a in _context.CustomerAdvanceAccounts.AsNoTracking()
                where a.CustGkey == customerGkey &&
                      a.Status == StatusActive &&
                      a.BalanceAmount > 0M
                join e in _context.SchemeEnrollments.AsNoTracking()
                    on a.Gkey equals e.AdvanceAccountGkey into ej
                from e in ej.DefaultIfEmpty()
                join o in _context.CustomerOrders.AsNoTracking()
                    on a.CustomerOrderGkey equals (int?)o.Gkey into oj
                from o in oj.DefaultIfEmpty()
                orderby a.OpenedOn, a.Gkey
                select new
                {
                    a.Gkey,
                    a.AccountNbr,
                    a.AccountType,
                    a.Status,
                    a.BalanceAmount,
                    a.BalanceWeight,
                    a.OpenedOn,
                    a.ValidUntil,
                    MaturityDate = (DateOnly?)e.MaturityDate,
                    OrderNbr = o.OrderNbr
                })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new AdvanceAccountSummary
            {
                AdvanceAccountGkey = x.Gkey,
                AccountNbr = x.AccountNbr,
                AccountType = x.AccountType,
                SettlementMode = SettlementModeFor(x.AccountType),
                Status = x.Status,
                BalanceAmount = x.BalanceAmount,
                BalanceWeight = x.BalanceWeight,
                OpenedOn = x.OpenedOn,
                ValidUntil = x.ValidUntil,
                MaturityDate = x.MaturityDate,
                CustomerOrderNbr = x.OrderNbr
            })
            .ToList();
    }

    // =========================================================
    // INVOICE ADJUSTMENT (DEBIT)
    // =========================================================

    public async Task ValidateInvoiceAdjustmentsAsync(
        int? customerGkey,
        IEnumerable<InvoiceSettlementSaveModel> adjustmentLines,
        CancellationToken cancellationToken = default)
    {
        foreach (var debit in GroupByAccount(adjustmentLines))
        {
            var account = await _context.CustomerAdvanceAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Gkey == debit.AccountGkey, cancellationToken)
                ?? throw new AdvanceAccountBusinessException(
                    $"Advance account {debit.AccountGkey} was not found.");

            EnsureUsable(account, customerGkey.GetValueOrDefault(), debit.Mode);

            if (account.BalanceAmount < debit.Amount)
                throw InsufficientBalance(account.AccountNbr, account.BalanceAmount, debit.Amount);
        }
    }

    public async Task<IReadOnlyList<AdvanceLedger>> ApplyInvoiceAdjustmentsAsync(
        InvoiceHeader invoice,
        IEnumerable<InvoiceSettlementSaveModel> adjustmentLines,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var debits = GroupByAccount(adjustmentLines);
        var added = new List<AdvanceLedger>();

        if (debits.Count == 0)
            return added;

        if (_context.Database.IsRelational() && _context.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "Advance adjustments must be applied inside the invoice transaction.");

        if (invoice.Gkey <= 0)
            throw new InvalidOperationException(
                "The invoice must be saved before advance adjustments are applied.");

        var identity = _auditIdentityProvider.GetCurrentIdentity();
        var now = DateTime.Now;
        var entryDate = DateOnly.FromDateTime(invoice.InvDate ?? now);

        foreach (var debit in debits)
        {
            var account = await _context.CustomerAdvanceAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Gkey == debit.AccountGkey, cancellationToken)
                ?? throw new AdvanceAccountBusinessException(
                    $"Advance account {debit.AccountGkey} was not found.");

            EnsureUsable(account, invoice.CustGkey.GetValueOrDefault(), debit.Mode);

            var sourceRef = InvoiceAdjustmentSourceRef(invoice.Gkey, account.Gkey);

            var alreadyApplied = await _context.AdvanceLedgers
                .AsNoTracking()
                .Where(x => x.SourceRef == sourceRef)
                .Select(x => (decimal?)x.Amount)
                .FirstOrDefaultAsync(cancellationToken);

            if (alreadyApplied is decimal appliedAmount)
            {
                if (-appliedAmount != debit.Amount)
                    throw new AdvanceAccountBusinessException(
                        $"Advance account {account.AccountNbr} was already adjusted by " +
                        $"₹{-appliedAmount:N2} on this invoice; ₹{debit.Amount:N2} was requested.");

                continue;
            }

            // The balance check and the debit are one statement, so two invoices
            // using the same advance concurrently cannot both succeed.
            var updated = await _context.CustomerAdvanceAccounts
                .Where(x => x.Gkey == account.Gkey &&
                            x.Status == StatusActive &&
                            x.BalanceAmount >= debit.Amount)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.BalanceAmount, x => x.BalanceAmount - debit.Amount)
                    .SetProperty(x => x.ModifiedBy, identity)
                    .SetProperty(x => x.ModifiedOn, now),
                    cancellationToken);

            if (updated == 0)
            {
                var current = await _context.CustomerAdvanceAccounts
                    .AsNoTracking()
                    .Where(x => x.Gkey == account.Gkey)
                    .Select(x => new { x.BalanceAmount, x.Status })
                    .SingleAsync(cancellationToken);

                if (current.Status != StatusActive)
                    throw new AdvanceAccountBusinessException(
                        $"Advance account {account.AccountNbr} is no longer active.");

                throw InsufficientBalance(account.AccountNbr, current.BalanceAmount, debit.Amount);
            }

            var ledger = new AdvanceLedger
            {
                AdvanceAccountGkey = account.Gkey,
                EntryType = AdvanceLedgerEntryType.Adjustment,
                EntryDate = entryDate,
                Amount = -debit.Amount,
                Weight = 0M,
                InvoiceGkey = invoice.Gkey,
                InvoiceNbr = invoice.InvNbr,
                SourceRef = sourceRef,
                Narration = Truncate($"{debit.Mode} against invoice {invoice.InvNbr}", 250),
                CreatedBy = identity,
                CreatedOn = now
            };

            _context.AdvanceLedgers.Add(ledger);
            added.Add(ledger);
        }

        return added;
    }

    private sealed record AccountDebit(int AccountGkey, string Mode, decimal Amount);

    private static List<AccountDebit> GroupByAccount(
        IEnumerable<InvoiceSettlementSaveModel>? lines)
    {
        var adjustmentLines = (lines ?? Enumerable.Empty<InvoiceSettlementSaveModel>())
            .Where(x => IsAdjustmentMode(x.PaymentMode))
            .ToList();

        if (adjustmentLines.Any(x => x.AdvanceAccountGkey is null or <= 0))
            throw new AdvanceAccountBusinessException(
                "Select the advance / RD account for every Advance Adj and RD Adj line.");

        if (adjustmentLines.Any(x => x.Amount <= 0M))
            throw new AdvanceAccountBusinessException(
                "Advance / RD adjustment amount must be greater than zero.");

        var debits = new List<AccountDebit>();

        foreach (var group in adjustmentLines.GroupBy(x => x.AdvanceAccountGkey!.Value))
        {
            var modes = group
                .Select(x => x.PaymentMode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (modes.Count > 1)
                throw new AdvanceAccountBusinessException(
                    $"Advance account {group.Key} is used with both Advance Adj and RD Adj.");

            var mode = string.Equals(modes[0], RdAdjMode, StringComparison.OrdinalIgnoreCase)
                ? RdAdjMode
                : AdvanceAdjMode;

            debits.Add(new AccountDebit(group.Key, mode, group.Sum(x => x.Amount)));
        }

        return debits;
    }

    private static void EnsureUsable(
        CustomerAdvanceAccount account,
        int customerGkey,
        string mode)
    {
        if (account.CustGkey != customerGkey)
            throw new AdvanceAccountBusinessException(
                $"Advance account {account.AccountNbr} belongs to another customer.");

        if (account.Status != StatusActive)
            throw new AdvanceAccountBusinessException(
                $"Advance account {account.AccountNbr} is {account.Status}, not ACTIVE.");

        if (SettlementModeFor(account.AccountType) != mode)
            throw new AdvanceAccountBusinessException(
                $"Advance account {account.AccountNbr} is a {account.AccountType} account " +
                $"and cannot be used as {mode}.");
    }

    private static AdvanceAccountBusinessException InsufficientBalance(
        string accountNbr,
        decimal available,
        decimal requested) =>
        new($"Insufficient balance in advance account {accountNbr}. " +
            $"Available: ₹{available:N2}, requested: ₹{requested:N2}.");

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
