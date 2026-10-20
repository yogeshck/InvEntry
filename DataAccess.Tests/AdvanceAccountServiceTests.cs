using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Services;
using DataAccess.Services.Advances;
using InvEntry.Contracts.Advances;
using InvEntry.Contracts.Invoices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DataAccess.Tests;

/// <summary>
/// Runs on SQLite in memory so the conditional UPDATEs (ExecuteUpdate), the
/// unique SOURCE_REF index and transactions behave as they do on SQL Server.
/// </summary>
[TestFixture]
public sealed class AdvanceAccountServiceTests
{
    private const int CustomerGkey = 501;
    private const int OtherCustomerGkey = 502;

    private SqliteConnection _connection = null!;
    private MijmsContext _context = null!;
    private AdvanceAccountService _service = null!;
    private int _advanceLedgerGkey;
    private int _rdLedgerGkey;

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _context = CreateContext();
        _context.Database.EnsureCreated();

        _context.OrgCustomers.AddRange(
            new OrgCustomer { Gkey = CustomerGkey },
            new OrgCustomer { Gkey = OtherCustomerGkey });

        var advanceLedger = new MtblLedger { LedgerName = "Advance", LedgerAccountCode = 1000 };
        var rdLedger = new MtblLedger { LedgerName = "RD", LedgerAccountCode = 3000 };
        _context.MtblLedgers.AddRange(
            advanceLedger,
            rdLedger,
            new MtblLedger { LedgerName = "Cash", LedgerAccountCode = 2000 });

        _context.VoucherTypes.AddRange(
            new VoucherType
            {
                Gkey = 1,
                DocumentType = AdvanceReceiptType.AdvanceReceipt,
                DocNbrPrefix = "AR-", DocNbrLength = 4, LastUsedNumber = 7, IsActive = true
            },
            new VoucherType
            {
                Gkey = 2,
                DocumentType = AdvanceReceiptType.RecurringDeposit,
                DocNbrPrefix = "RD-", DocNbrLength = 4, LastUsedNumber = 0, IsActive = true
            });

        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        _advanceLedgerGkey = advanceLedger.Gkey;
        _rdLedgerGkey = rdLedger.Gkey;

        _service = CreateService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // =========================================================
    // RECEIPT
    // =========================================================

    [Test]
    public async Task Receipt_creates_voucher_ledger_row_and_balance_together()
    {
        var request = Receipt(AdvanceReceiptType.AdvanceReceipt, 5000M);

        var response = await _service.CreateReceiptAsync(request);

        var voucher = await _context.Vouchers.AsNoTracking().SingleAsync();
        var ledger = await _context.AdvanceLedgers.AsNoTracking().SingleAsync();
        var account = await _context.CustomerAdvanceAccounts.AsNoTracking().SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.IsReplay, Is.False);
            Assert.That(response.VoucherNbr, Is.EqualTo("AR-0008"));
            Assert.That(response.BalanceAmount, Is.EqualTo(5000M));

            Assert.That(voucher.TransType, Is.EqualTo("Receipt"));
            Assert.That(voucher.VoucherType, Is.EqualTo("Advance Receipt"));
            Assert.That(voucher.CustomerGkey, Is.EqualTo(CustomerGkey));
            Assert.That(voucher.ToLedgerGkey, Is.EqualTo(_advanceLedgerGkey));
            Assert.That(voucher.TransAmount, Is.EqualTo(5000M));
            Assert.That(voucher.Mode, Is.EqualTo("Cash"));
            Assert.That(voucher.SeqNbr, Is.EqualTo(1));

            Assert.That(account.AccountType, Is.EqualTo("OPEN"));
            Assert.That(account.AccountNbr, Is.EqualTo($"ADV-{account.Gkey:D6}"));
            Assert.That(account.MtblLedgersGkey, Is.EqualTo(_advanceLedgerGkey));
            Assert.That(account.BalanceAmount, Is.EqualTo(5000M));
            Assert.That(account.TotalReceivedAmount, Is.EqualTo(5000M));

            Assert.That(ledger.EntryType, Is.EqualTo("RECEIPT"));
            Assert.That(ledger.Amount, Is.EqualTo(5000M));
            Assert.That(ledger.VoucherGkey, Is.EqualTo(voucher.Gkey));
            Assert.That(ledger.AdvanceAccountGkey, Is.EqualTo(account.Gkey));
            Assert.That(ledger.SourceRef,
                Is.EqualTo(AdvanceAccountService.ReceiptSourceRef(request.ClientRequestId)));
        });
    }

    [Test]
    public async Task Second_receipt_adds_to_the_same_account()
    {
        await _service.CreateReceiptAsync(Receipt(AdvanceReceiptType.AdvanceReceipt, 5000M));
        var second = await _service.CreateReceiptAsync(Receipt(AdvanceReceiptType.AdvanceReceipt, 2500M));

        var account = await _context.CustomerAdvanceAccounts.AsNoTracking().SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(second.BalanceAmount, Is.EqualTo(7500M));
            Assert.That(account.BalanceAmount, Is.EqualTo(7500M));
            Assert.That(account.TotalReceivedAmount, Is.EqualTo(7500M));
            Assert.That(_context.AdvanceLedgers.Count(), Is.EqualTo(2));
            Assert.That(_context.Vouchers.Count(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Retried_receipt_with_same_request_id_is_recorded_once()
    {
        var request = Receipt(AdvanceReceiptType.RecurringDeposit, 1000M);

        var first = await _service.CreateReceiptAsync(request);

        // A fresh context, as a retried HTTP request would get.
        using var retryContext = CreateContext();
        var retry = await CreateService(retryContext).CreateReceiptAsync(request);

        var account = await _context.CustomerAdvanceAccounts.AsNoTracking().SingleAsync();
        var voucherType = await _context.VoucherTypes.AsNoTracking()
            .SingleAsync(x => x.DocumentType == AdvanceReceiptType.RecurringDeposit);

        Assert.Multiple(() =>
        {
            Assert.That(retry.IsReplay, Is.True);
            Assert.That(retry.VoucherGkey, Is.EqualTo(first.VoucherGkey));
            Assert.That(retry.VoucherNbr, Is.EqualTo(first.VoucherNbr));
            Assert.That(retry.AdvanceLedgerGkey, Is.EqualTo(first.AdvanceLedgerGkey));
            Assert.That(_context.Vouchers.Count(), Is.EqualTo(1));
            Assert.That(_context.AdvanceLedgers.Count(), Is.EqualTo(1));
            Assert.That(account.AccountType, Is.EqualTo("SCHEME"));
            Assert.That(account.MtblLedgersGkey, Is.EqualTo(_rdLedgerGkey));
            Assert.That(account.BalanceAmount, Is.EqualTo(1000M));
            Assert.That(account.TotalReceivedAmount, Is.EqualTo(1000M));
            Assert.That(voucherType.LastUsedNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Receipt_into_a_closed_account_writes_nothing()
    {
        var account = await AddAccountAsync("OPEN", 0M, status: "CLOSED");

        var request = Receipt(AdvanceReceiptType.AdvanceReceipt, 100M);
        request.AdvanceAccountGkey = account.Gkey;

        Assert.ThrowsAsync<AdvanceAccountBusinessException>(
            () => _service.CreateReceiptAsync(request));

        Assert.Multiple(() =>
        {
            Assert.That(_context.Vouchers.Count(), Is.Zero);
            Assert.That(_context.AdvanceLedgers.Count(), Is.Zero);
            Assert.That(_context.VoucherTypes.AsNoTracking()
                .Single(x => x.DocumentType == AdvanceReceiptType.AdvanceReceipt)
                .LastUsedNumber, Is.EqualTo(7));
        });
    }

    [Test]
    public async Task Rd_receipt_cannot_credit_an_advance_account()
    {
        var account = await AddAccountAsync("OPEN", 0M);

        var request = Receipt(AdvanceReceiptType.RecurringDeposit, 100M);
        request.AdvanceAccountGkey = account.Gkey;

        Assert.ThrowsAsync<AdvanceAccountBusinessException>(
            () => _service.CreateReceiptAsync(request));
    }

    // =========================================================
    // INVOICE DEBIT
    // =========================================================

    [Test]
    public async Task Invoice_adjustment_debits_balance_and_adds_adjustment_row()
    {
        var account = await AddAccountAsync("OPEN", 5000M);
        var invoice = Invoice(9001);

        await using (var tx = await _context.Database.BeginTransactionAsync())
        {
            await _service.ApplyInvoiceAdjustmentsAsync(invoice, new[]
            {
                Adj(AdvanceAccountService.AdvanceAdjMode, account.Gkey, 1500M),
                Adj(AdvanceAccountService.AdvanceAdjMode, account.Gkey, 500M),
                new InvoiceSettlementSaveModel { PaymentMode = "Cash", Amount = 999M }
            });
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }

        var ledger = await _context.AdvanceLedgers.AsNoTracking().SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(Balance(account.Gkey), Is.EqualTo(3000M));
            Assert.That(ledger.EntryType, Is.EqualTo("ADJUSTMENT"));
            Assert.That(ledger.Amount, Is.EqualTo(-2000M));
            Assert.That(ledger.InvoiceGkey, Is.EqualTo(9001));
            Assert.That(ledger.InvoiceNbr, Is.EqualTo("B9001"));
            Assert.That(ledger.SourceRef, Is.EqualTo($"INVOICE-9001-ADJ-{account.Gkey}"));
        });
    }

    [Test]
    public async Task Insufficient_balance_fails_and_rolls_back()
    {
        var account = await AddAccountAsync("SCHEME", 1000M);
        var lines = new[] { Adj(AdvanceAccountService.RdAdjMode, account.Gkey, 1000.01M) };

        Assert.ThrowsAsync<AdvanceAccountBusinessException>(
            () => _service.ValidateInvoiceAdjustmentsAsync(CustomerGkey, lines));

        await using (var tx = await _context.Database.BeginTransactionAsync())
        {
            var ex = Assert.ThrowsAsync<AdvanceAccountBusinessException>(
                () => _service.ApplyInvoiceAdjustmentsAsync(Invoice(9002), lines));
            Assert.That(ex!.Message, Does.Contain("Insufficient balance"));
            await tx.RollbackAsync();
        }

        _context.ChangeTracker.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(Balance(account.Gkey), Is.EqualTo(1000M));
            Assert.That(_context.AdvanceLedgers.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task Balance_used_by_another_invoice_after_validation_fails_the_second_debit()
    {
        var account = await AddAccountAsync("OPEN", 3000M);
        var lines = new[] { Adj(AdvanceAccountService.AdvanceAdjMode, account.Gkey, 2000M) };

        // Both invoices pass validation against the same cached balance.
        await _service.ValidateInvoiceAdjustmentsAsync(CustomerGkey, lines);
        await _service.ValidateInvoiceAdjustmentsAsync(CustomerGkey, lines);

        await using (var tx = await _context.Database.BeginTransactionAsync())
        {
            await _service.ApplyInvoiceAdjustmentsAsync(Invoice(9003), lines);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }

        await using (var tx = await _context.Database.BeginTransactionAsync())
        {
            Assert.ThrowsAsync<AdvanceAccountBusinessException>(
                () => _service.ApplyInvoiceAdjustmentsAsync(Invoice(9004), lines));
            await tx.RollbackAsync();
        }

        _context.ChangeTracker.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(Balance(account.Gkey), Is.EqualTo(1000M));
            Assert.That(_context.AdvanceLedgers.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Retried_invoice_adjustment_debits_once()
    {
        var account = await AddAccountAsync("OPEN", 5000M);
        var lines = new[] { Adj(AdvanceAccountService.AdvanceAdjMode, account.Gkey, 1200M) };

        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            await _service.ApplyInvoiceAdjustmentsAsync(Invoice(9005), lines);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }

        Assert.Multiple(() =>
        {
            Assert.That(Balance(account.Gkey), Is.EqualTo(3800M));
            Assert.That(_context.AdvanceLedgers.Count(), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Adjustment_line_must_name_an_account_of_the_invoice_customer_and_matching_mode()
    {
        var advance = await AddAccountAsync("OPEN", 5000M);
        var others = await AddAccountAsync("OPEN", 5000M, customerGkey: OtherCustomerGkey);

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<AdvanceAccountBusinessException>(() =>
                _service.ValidateInvoiceAdjustmentsAsync(CustomerGkey,
                    new[] { Adj(AdvanceAccountService.AdvanceAdjMode, null, 100M) }));

            Assert.ThrowsAsync<AdvanceAccountBusinessException>(() =>
                _service.ValidateInvoiceAdjustmentsAsync(CustomerGkey,
                    new[] { Adj(AdvanceAccountService.AdvanceAdjMode, others.Gkey, 100M) }));

            Assert.ThrowsAsync<AdvanceAccountBusinessException>(() =>
                _service.ValidateInvoiceAdjustmentsAsync(CustomerGkey,
                    new[] { Adj(AdvanceAccountService.RdAdjMode, advance.Gkey, 100M) }));
        });
    }

    // =========================================================
    // AVAILABLE ACCOUNTS
    // =========================================================

    [Test]
    public async Task Available_accounts_are_active_with_balance_and_carry_settlement_mode()
    {
        var advance = await AddAccountAsync("OPEN", 2500M);
        var rd = await AddAccountAsync("SCHEME", 6000M);
        await AddAccountAsync("OPEN", 0M);
        await AddAccountAsync("OPEN", 900M, status: "CLOSED");
        await AddAccountAsync("OPEN", 700M, customerGkey: OtherCustomerGkey);

        var accounts = await _service.GetAvailableAccountsAsync(CustomerGkey);

        Assert.That(accounts.Select(x => (x.AdvanceAccountGkey, x.SettlementMode, x.BalanceAmount)),
            Is.EquivalentTo(new[]
            {
                (advance.Gkey, "Advance Adj", 2500M),
                (rd.Gkey, "RD Adj", 6000M)
            }));
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private MijmsContext CreateContext() =>
        new SqliteMijmsContext(new DbContextOptionsBuilder<MijmsContext>()
            .UseSqlite(_connection)
            .Options);

    private static AdvanceAccountService CreateService(MijmsContext context) =>
        new(context,
            new VoucherNumberService(new RepositoryBase<VoucherType>(context)),
            new AuditIdentityProvider());

    private static AdvanceReceiptRequest Receipt(string type, decimal amount) => new()
    {
        ClientRequestId = Guid.NewGuid(),
        CustomerGkey = CustomerGkey,
        ReceiptType = type,
        Amount = amount,
        Mode = "Cash",
        TransDesc = "test receipt"
    };

    private static InvoiceSettlementSaveModel Adj(string mode, int? accountGkey, decimal amount) => new()
    {
        SettlementType = InvoiceSettlementType.Receipt,
        PaymentMode = mode,
        AdvanceAccountGkey = accountGkey,
        Amount = amount
    };

    private static InvoiceHeader Invoice(int gkey) => new()
    {
        Gkey = gkey,
        InvNbr = $"B{gkey}",
        InvDate = DateTime.Today,
        CustGkey = CustomerGkey
    };

    private async Task<CustomerAdvanceAccount> AddAccountAsync(
        string type,
        decimal balance,
        string status = "ACTIVE",
        int customerGkey = CustomerGkey)
    {
        var account = new CustomerAdvanceAccount
        {
            AccountNbr = $"T-{Guid.NewGuid():N}",
            AccountType = type,
            CustGkey = customerGkey,
            MtblLedgersGkey = type == "SCHEME" ? _rdLedgerGkey : _advanceLedgerGkey,
            Status = status,
            OpenedOn = DateOnly.FromDateTime(DateTime.Today),
            ClosedOn = status == "CLOSED" ? DateOnly.FromDateTime(DateTime.Today) : null,
            BalanceAmount = balance,
            TotalReceivedAmount = balance,
            CreatedBy = "TEST",
            CreatedOn = DateTime.Now
        };

        _context.CustomerAdvanceAccounts.Add(account);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return account;
    }

    private decimal Balance(int accountGkey) =>
        _context.CustomerAdvanceAccounts.AsNoTracking()
            .Single(x => x.Gkey == accountGkey).BalanceAmount;

    /// <summary>
    /// SQLite has no ROWVERSION and stores decimals as text (compared as text),
    /// so the test model generates the row version and stores money as REAL.
    /// </summary>
    private sealed class SqliteMijmsContext(DbContextOptions<MijmsContext> options)
        : MijmsContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomerAdvanceAccount>()
                .Property(x => x.RowVersion)
                .HasDefaultValueSql("randomblob(8)");

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(x => x.GetProperties())
                         .Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?)))
            {
                property.SetValueConverter(property.ClrType == typeof(decimal)
                    ? new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<decimal, double>(
                        v => (double)v, v => (decimal)v)
                    : new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<decimal?, double?>(
                        v => (double?)v, v => (decimal?)v));
                property.SetColumnType("REAL");
            }
        }
    }
}
