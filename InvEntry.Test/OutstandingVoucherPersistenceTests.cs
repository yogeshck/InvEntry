using DataAccess.Controllers;
using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvEntry.Test;

// "Apply Receipt" on the outstanding-voucher screen posts a voucher and an AR receipt.
// Both endpoints used to Add() without SaveChanges, so nothing reached the database.
[TestFixture]
public class OutstandingVoucherPersistenceTests
{
    [Test]
    public async Task VoucherPost_PersistsVoucherAndAdvancesNumbering()
    {
        string db = Guid.NewGuid().ToString();
        using (var seed = new TestMijmsContext(db))
        {
            seed.VoucherTypes.Add(new VoucherType { DocumentType = "Cash", DocNbrPrefix = "Cash-", LastUsedNumber = 7 });
            await seed.SaveChangesAsync();
        }

        ActionResult<Voucher> result;
        using (var context = new TestMijmsContext(db))
        {
            var controller = new VoucherController(new RepositoryBase<Voucher>(context),
                                                   new RepositoryBase<VoucherType>(context),
                                                   new UnitOfWork(context));
            result = await controller.Post(new Voucher { VoucherType = "Cash", VoucherDate = DateTime.Now, TransAmount = 900m });
        }

        using var verify = new TestMijmsContext(db);
        var saved = await verify.Vouchers.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Value?.Gkey, Is.GreaterThan(0));
            Assert.That(saved.VoucherNbr, Is.EqualTo("Cash-0008"));
            Assert.That(saved.TransAmount, Is.EqualTo(900m));
            Assert.That(verify.VoucherTypes.Single().LastUsedNumber, Is.EqualTo(8));
        });
    }

    [Test]
    public async Task VoucherPost_UnknownVoucherType_IsRejectedInsteadOfCrashing()
    {
        using var context = new TestMijmsContext(Guid.NewGuid().ToString());
        var controller = new VoucherController(new RepositoryBase<Voucher>(context),
                                               new RepositoryBase<VoucherType>(context),
                                               new UnitOfWork(context));

        var result = await controller.Post(new Voucher { VoucherType = "Nope", VoucherDate = DateTime.Now });

        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        Assert.That(context.Vouchers.Count(), Is.Zero);
    }

    [Test]
    public async Task ReceiptPost_PersistsAndLookupByInvoiceNumberExcludesOtherDocuments()
    {
        string db = Guid.NewGuid().ToString();
        using (var seed = new TestMijmsContext(db))
        {
            // Invoice and estimate headers share gkey values, so receipts for both carry InvoiceGkey 29598.
            seed.InvoiceArReceipts.Add(new InvoiceArReceipt { InvoiceGkey = 29598, InvoiceNbr = "E-00210", SeqNbr = 1, TransactionType = "Credit", AdjustedAmount = 61861m });
            await seed.SaveChangesAsync();
        }

        using (var context = new TestMijmsContext(db))
        {
            var controller = new InvoiceArReceiptController(new RepositoryBase<InvoiceArReceipt>(context), new UnitOfWork(context));
            await controller.Post(new InvoiceArReceipt { InvoiceGkey = 29598, InvoiceNbr = "D-00932", SeqNbr = 2, TransactionType = "Cash", AdjustedAmount = 5000m });
        }

        using var verify = new TestMijmsContext(db);
        var lookup = new InvoiceArReceiptController(new RepositoryBase<InvoiceArReceipt>(verify), new UnitOfWork(verify));

        var forInvoice = lookup.GetByInvHdrGKey(29598, "D-00932").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(verify.InvoiceArReceipts.Count(), Is.EqualTo(2));
            Assert.That(forInvoice.Select(x => x.InvoiceNbr), Is.EqualTo(new[] { "D-00932" }));
            Assert.That(forInvoice.Single().AdjustedAmount, Is.EqualTo(5000m));
            Assert.That(lookup.GetByInvHdrGKey(29598).Count(), Is.EqualTo(2));
        });
    }

    private sealed class TestMijmsContext(string databaseName) : MijmsContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseInMemoryDatabase(databaseName);
    }
}
