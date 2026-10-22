using DataAccess.Controllers;
using DataAccess.Models;
using DataAccess.Models.FinanceIntegration;
using DataAccess.Repository;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace DataAccess.Tests;

// Money received against a credit invoice after finalisation is posted to FinanceTracker
// as its own COLLECTION event, on the day it was received.
public sealed class FinanceCollectionSyncTests
{
    private const int InvoiceGkey = 29964;
    private const string InvoiceNbr = "D-01292";

    [TestCase("Cash", "Cash", "CASH")]
    [TestCase("GPAY", "GPAY", "NON_CASH")]
    [TestCase("Credit Card", "Credit Card", "NON_CASH")]
    [TestCase("Discount", "Discount", "WRITE_OFF")]
    [TestCase("Discount", "Cash", "WRITE_OFF")] // older discount vouchers were saved with Mode = Cash
    public async Task Collection_payload_is_dated_when_received_and_refers_to_the_invoice(
        string voucherType, string mode, string expectedGroup)
    {
        await using var context = CreateContext();
        SeedInvoice(context);
        var voucher = SeedVoucher(context, voucherType, mode, 2000m);
        await context.SaveChangesAsync();

        var payload = await new FinanceInvoicePayloadBuilder(context)
            .BuildCollectionAsync(voucher.Gkey, 1, 1);

        Assert.That(payload, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(payload!.DocumentType, Is.EqualTo("COLLECTION"));
            Assert.That(payload.SourceEventId, Is.EqualTo($"VOUCHER-{voucher.Gkey}-COLLECTION"));
            Assert.That(payload.DocumentNo, Is.EqualTo("V-0001"));
            Assert.That(payload.DocumentDate, Is.EqualTo(new DateOnly(2026, 10, 22)));
            Assert.That(payload.SourceDocumentNo, Is.EqualTo(InvoiceNbr));
            Assert.That(payload.DocumentAmount, Is.EqualTo(2000m));
            Assert.That(payload.Movements.Single().PaymentGroup, Is.EqualTo(expectedGroup));
            Assert.That(payload.Movements.Single().SourceReference, Is.EqualTo(voucher.Gkey.ToString()));
        });
    }

    [Test]
    public async Task Voucher_of_an_estimate_sharing_the_invoice_gkey_is_not_a_collection()
    {
        await using var context = CreateContext();
        SeedInvoice(context);
        var voucher = SeedVoucher(context, "Cash", "Cash", 500m, refDocNbr: "E-00210");
        await context.SaveChangesAsync();

        var payload = await new FinanceInvoicePayloadBuilder(context)
            .BuildCollectionAsync(voucher.Gkey, 1, 1);

        Assert.That(payload, Is.Null);
    }

    [Test]
    public async Task Receipt_on_an_invoice_that_is_not_finalised_is_not_a_collection()
    {
        await using var context = CreateContext();
        SeedInvoice(context, status: "DRAFT");
        var voucher = SeedVoucher(context, "Cash", "Cash", 500m);
        await context.SaveChangesAsync();

        Assert.That(await new FinanceInvoicePayloadBuilder(context).BuildCollectionAsync(voucher.Gkey, 1, 1), Is.Null);
    }

    [Test]
    public async Task Collection_is_queued_once_with_the_invoice_number_for_searching()
    {
        await using var context = CreateContext();
        SeedInvoice(context);
        var voucher = SeedVoucher(context, "Cash", "Cash", 30000m);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var first = await service.QueueCollectionSyncAsync(voucher.Gkey, 1, 1);
        var second = await service.QueueCollectionSyncAsync(voucher.Gkey, 1, 1);

        var row = await context.FinanceSyncOutboxes.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(FinanceQueueResult.Queued));
            Assert.That(second, Is.EqualTo(FinanceQueueResult.AlreadyQueued));
            Assert.That(row.SourceType, Is.EqualTo("COLLECTION"));
            Assert.That(row.SourceEventId, Is.EqualTo($"VOUCHER-{voucher.Gkey}-COLLECTION"));
            Assert.That(row.DocumentNo, Is.EqualTo(InvoiceNbr));
            Assert.That(row.DocumentDate, Is.EqualTo(new DateTime(2026, 10, 22)));
            Assert.That(row.Status, Is.EqualTo(FinanceSyncService.Pending));
        });
    }

    [Test]
    public async Task Queued_collection_dispatches_through_the_collection_posting()
    {
        await using var context = CreateContext();
        SeedInvoice(context);
        var voucher = SeedVoucher(context, "GPAY", "GPAY", 2000m);
        await context.SaveChangesAsync();
        var poster = new RecordingPoster();
        var service = CreateService(context, poster);
        await service.QueueCollectionSyncAsync(voucher.Gkey, 1, 1);
        var row = await context.FinanceSyncOutboxes.SingleAsync();

        var sent = await service.DispatchClaimedAsync(row, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.True);
            Assert.That(row.Status, Is.EqualTo(FinanceSyncService.Sent));
            Assert.That(poster.Posted.Single().DocumentType, Is.EqualTo("COLLECTION"));
        });
    }

    [Test]
    public async Task Outstanding_adjustment_voucher_is_saved_with_its_collection_event()
    {
        string db = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(db))
        {
            SeedInvoice(seed);
            seed.VoucherTypes.Add(new VoucherType { DocumentType = "Cash", DocNbrPrefix = "Cash-", LastUsedNumber = 727 });
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext(db))
        {
            var controller = CreateVoucherController(context, enabled: true);
            await controller.Post(OutstandingAdjustment("Cash", 30000m));
        }

        await using var verify = CreateContext(db);
        var voucher = await verify.Vouchers.SingleAsync();
        var row = await verify.FinanceSyncOutboxes.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(voucher.VoucherNbr, Is.EqualTo("Cash-0728"));
            Assert.That(row.SourceEventId, Is.EqualTo($"VOUCHER-{voucher.Gkey}-COLLECTION"));
        });
    }

    [Test]
    public async Task Voucher_is_saved_without_an_event_when_finance_sync_is_off()
    {
        string db = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(db))
        {
            SeedInvoice(seed);
            seed.VoucherTypes.Add(new VoucherType { DocumentType = "Cash", DocNbrPrefix = "Cash-", LastUsedNumber = 1 });
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext(db))
            await CreateVoucherController(context, enabled: false).Post(OutstandingAdjustment("Cash", 100m));

        await using var verify = CreateContext(db);
        Assert.Multiple(() =>
        {
            Assert.That(verify.Vouchers.Count(), Is.EqualTo(1));
            Assert.That(verify.FinanceSyncOutboxes.Count(), Is.Zero);
        });
    }

    private static Voucher OutstandingAdjustment(string paymentType, decimal amount) => new()
    {
        VoucherType = paymentType,
        TransType = "Receipt",
        Mode = paymentType,
        VoucherDate = new DateTime(2026, 10, 22, 11, 0, 0),
        TransDate = new DateTime(2026, 10, 22),
        RefDocGkey = InvoiceGkey,
        RefDocNbr = InvoiceNbr,
        TransAmount = amount
    };

    private static VoucherController CreateVoucherController(MijmsContext context, bool enabled) =>
        new(new RepositoryBase<Voucher>(context),
            new RepositoryBase<VoucherType>(context),
            new UnitOfWork(context),
            CreateService(context),
            Options.Create(new FinanceSyncOptions { Enabled = enabled, OrgGkey = 1, LocationGkey = 1 }));

    private static void SeedInvoice(MijmsContext context, string status = "FINALISED") =>
        context.InvoiceHeaders.Add(new InvoiceHeader
        {
            Gkey = InvoiceGkey,
            InvNbr = InvoiceNbr,
            InvDate = new DateTime(2026, 10, 16, 6, 10, 0),
            AmountPayable = 33166m,
            Status = status
        });

    private static Voucher SeedVoucher(MijmsContext context, string voucherType, string mode,
        decimal amount, string refDocNbr = InvoiceNbr)
    {
        var voucher = new Voucher
        {
            VoucherNbr = "V-0001",
            VoucherType = voucherType,
            TransType = "Receipt",
            Mode = mode,
            TransAmount = amount,
            VoucherDate = new DateTime(2026, 10, 22, 11, 0, 0),
            TransDate = new DateTime(2026, 10, 22),
            RefDocGkey = InvoiceGkey,
            RefDocNbr = refDocNbr,
            TransDesc = $"{voucherType}-Receipt-{mode}"
        };
        context.Vouchers.Add(voucher);
        return voucher;
    }

    private static MijmsContext CreateContext(string? databaseName = null) =>
        new(new DbContextOptionsBuilder<MijmsContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            // The in-memory provider has no transactions; the controller still opens one.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static FinanceSyncService CreateService(MijmsContext context,
        IFinanceDocumentPoster? poster = null) =>
        new(context, new FinanceInvoicePayloadBuilder(context),
            poster ?? new RecordingPoster(), new NotPostedStatusReader());

    private sealed class RecordingPoster : IFinanceDocumentPoster
    {
        public List<FinanceDocumentPayload> Posted { get; } = [];

        public Task<FinancePostingResult> PostAsync(FinanceDocumentPayload payload,
            CancellationToken cancellationToken)
        {
            Posted.Add(payload);
            return Task.FromResult(new FinancePostingResult(FinancePostingOutcome.Posted, null));
        }
    }

    private sealed class NotPostedStatusReader : IFinanceDocumentStatusReader
    {
        public Task<FinanceRemoteEventState> GetStatusAsync(int orgGkey, string sourceSystem,
            string sourceEventId, CancellationToken cancellationToken) =>
            Task.FromResult(FinanceRemoteEventState.NotPosted);
    }
}
