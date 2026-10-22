using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using DataAccess.Repository;
using DataAccess.Services;
using DataAccess.Workflows;
using InvEntry.Contracts.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace DataAccess.Tests;

// Outstanding-invoice screen: several receipts entered in a grid and saved as one unit.
public sealed class InvoiceCollectionWorkflowTests
{
    private const int InvoiceGkey = 29977;
    private const string InvoiceNbr = "D-01303";

    [Test]
    public async Task Receipts_clearing_the_balance_are_saved_together_with_their_finance_events()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, balance: 2749m, received: 42000m);

        SaveInvoiceCollectionResponse response;
        await using (var context = CreateContext(db))
        {
            response = await CreateWorkflow(context).SaveAsync(Request(
                ("Cash", 2000m, null),
                ("GPAY", 583m, "UTR998877"),
                ("Discount", 166m, null)));
        }

        await using var verify = CreateContext(db);
        var invoice = await verify.InvoiceHeaders.SingleAsync();
        var receipts = await verify.InvoiceArReceipts.OrderBy(x => x.SeqNbr).ToListAsync();
        var vouchers = await verify.Vouchers.OrderBy(x => x.Gkey).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.InvBalance, Is.Zero);
            Assert.That(response.VoucherNumbers, Is.EqualTo(new[] { "Cash-0734", "GPAY-0067", "Disc-1198" }));

            Assert.That(invoice.InvBalance, Is.Zero);
            Assert.That(invoice.RecdAmount, Is.EqualTo(44749m));

            // Continues after the existing receipt, each line from the balance the previous one left.
            Assert.That(receipts.Select(x => x.SeqNbr), Is.EqualTo(new[] { 2, 3, 4, 5 }));
            Assert.That(receipts.Skip(1).Select(x => x.BalanceAfterAdj), Is.EqualTo(new decimal?[] { 749m, 166m, 0m }));
            Assert.That(receipts[2].ExternalTransactionId, Is.EqualTo("UTR998877"));

            Assert.That(vouchers.Select(x => x.Mode), Is.EqualTo(new[] { "Cash", "GPAY", "Discount" }));
            Assert.That(vouchers.All(x => x.TransDate == new DateTime(2026, 10, 22)), Is.True);
            Assert.That(vouchers.All(x => x.RefDocGkey == InvoiceGkey && x.RefDocNbr == InvoiceNbr), Is.True);

            Assert.That(verify.FinanceSyncOutboxes.Select(x => x.SourceEventId),
                Is.EquivalentTo(vouchers.Select(x => $"VOUCHER-{x.Gkey}-COLLECTION")));
        });
    }

    [Test]
    public async Task More_than_the_balance_is_rejected_and_nothing_is_saved()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, balance: 2749m, received: 42000m);

        await using (var context = CreateContext(db))
        {
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreateWorkflow(context).SaveAsync(Request(("Cash", 4000m, null))));
            Assert.That(ex!.Message, Does.Contain("more than the balance"));
        }

        await using var verify = CreateContext(db);
        Assert.Multiple(() =>
        {
            Assert.That(verify.Vouchers.Count(), Is.Zero);
            Assert.That(verify.InvoiceArReceipts.Count(), Is.EqualTo(1));
            Assert.That(verify.InvoiceHeaders.Single().InvBalance, Is.EqualTo(2749m));
            Assert.That(verify.VoucherTypes.Single(x => x.DocumentType == "Cash").LastUsedNumber, Is.EqualTo(733));
            Assert.That(verify.FinanceSyncOutboxes.Count(), Is.Zero);
        });
    }

    [Test]
    public async Task Invoice_is_matched_by_number_as_well_as_gkey()
    {
        string db = Guid.NewGuid().ToString();
        await SeedAsync(db, balance: 2749m, received: 42000m);

        await using var context = CreateContext(db);
        var request = Request(("Cash", 100m, null));
        request.InvNbr = "E-00210"; // an estimate sharing the invoice's gkey

        Assert.ThrowsAsync<KeyNotFoundException>(() => CreateWorkflow(context).SaveAsync(request));
    }

    private static SaveInvoiceCollectionRequest Request(params (string Type, decimal Amount, string? Reference)[] lines) => new()
    {
        InvoiceGkey = InvoiceGkey,
        InvNbr = InvoiceNbr,
        ReceivedOn = new DateTime(2026, 10, 22),
        Lines = lines.Select(x => new InvoiceCollectionLineModel
        {
            PaymentType = x.Type, Amount = x.Amount, Reference = x.Reference
        }).ToList()
    };

    private static async Task SeedAsync(string db, decimal balance, decimal received)
    {
        await using var seed = CreateContext(db);
        seed.InvoiceHeaders.Add(new InvoiceHeader
        {
            Gkey = InvoiceGkey, InvNbr = InvoiceNbr, InvDate = new DateTime(2026, 10, 19),
            AmountPayable = received + balance, RecdAmount = received, InvBalance = balance,
            CustGkey = 7, Status = "FINALISED"
        });
        seed.InvoiceArReceipts.Add(new InvoiceArReceipt
        {
            InvoiceGkey = InvoiceGkey, InvoiceNbr = InvoiceNbr, SeqNbr = 2,
            TransactionType = "Credit", AdjustedAmount = 0m, BalanceAfterAdj = balance
        });
        seed.VoucherTypes.AddRange(
            new VoucherType { Gkey = 1, DocumentType = "Cash", DocNbrPrefix = "Cash-", LastUsedNumber = 733 },
            new VoucherType { Gkey = 2, DocumentType = "GPAY", DocNbrPrefix = "GPAY-", LastUsedNumber = 66 },
            new VoucherType { Gkey = 3, DocumentType = "Discount", DocNbrPrefix = "Disc-", LastUsedNumber = 1197 });
        await seed.SaveChangesAsync();
    }

    private static InvoiceCollectionWorkflow CreateWorkflow(MijmsContext context) =>
        new(new RepositoryBase<InvoiceHeader>(context),
            new RepositoryBase<InvoiceArReceipt>(context),
            new RepositoryBase<Voucher>(context),
            new RepositoryBase<VoucherType>(context),
            new UnitOfWork(context),
            new FinanceSyncService(context, new FinanceInvoicePayloadBuilder(context),
                new NoPoster(), new NoStatusReader()),
            Options.Create(new FinanceSyncOptions { Enabled = true, OrgGkey = 1, LocationGkey = 1 }));

    private static MijmsContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<MijmsContext>()
            .UseInMemoryDatabase(databaseName)
            // The in-memory provider has no transactions, so a rejected save is
            // proven here by the workflow validating before it stages anything.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private sealed class NoPoster : IFinanceDocumentPoster
    {
        public Task<FinancePostingResult> PostAsync(FinanceDocumentPayload payload, CancellationToken cancellationToken) =>
            Task.FromResult(new FinancePostingResult(FinancePostingOutcome.Posted, null));
    }

    private sealed class NoStatusReader : IFinanceDocumentStatusReader
    {
        public Task<FinanceRemoteEventState> GetStatusAsync(int orgGkey, string sourceSystem,
            string sourceEventId, CancellationToken cancellationToken) =>
            Task.FromResult(FinanceRemoteEventState.NotPosted);
    }
}
