using DataAccess.Controllers;
using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.EntityFrameworkCore;

namespace InvEntry.Test;

// RepositoryBase only stages changes, so every write endpoint has to commit through IUnitOfWork.
// Each test writes through one controller and reads back from a fresh context to prove the row reached the store.
[TestFixture]
public class ControllerPersistenceTests
{
    [Test]
    public async Task InvoiceLine_PostPutDelete_ArePersisted()
    {
        string db = Guid.NewGuid().ToString();

        int gkey;
        using (var context = new TestMijmsContext(db))
        {
            var created = await NewInvoiceLineController(context).Post(new InvoiceLine { InvLineNbr = 1, InvoiceHdrGkey = 10, ProductGkey = 5 });
            gkey = created.Gkey;
        }

        using (var verify = new TestMijmsContext(db))
            Assert.That(verify.InvoiceLines.Single().InvoiceHdrGkey, Is.EqualTo(10));

        using (var context = new TestMijmsContext(db))
            await NewInvoiceLineController(context).Put(2, new InvoiceLine { Gkey = gkey, InvoiceHdrGkey = 10, ProductGkey = 6 });

        using (var verify = new TestMijmsContext(db))
        {
            var updated = verify.InvoiceLines.Single();
            Assert.Multiple(() =>
            {
                Assert.That(updated.InvLineNbr, Is.EqualTo(2));
                Assert.That(updated.ProductGkey, Is.EqualTo(6));
                Assert.That(updated.ModifiedOn, Is.Not.Null);
            });
        }

        using (var context = new TestMijmsContext(db))
            await NewInvoiceLineController(context).Delete(2);

        using (var verify = new TestMijmsContext(db))
            Assert.That(verify.InvoiceLines.Count(), Is.Zero);
    }

    [Test]
    public void InvoiceLine_DeleteUnknownLine_IsANoOp()
    {
        using var context = new TestMijmsContext(Guid.NewGuid().ToString());

        Assert.DoesNotThrowAsync(() => NewInvoiceLineController(context).Delete(99));
    }

    [Test]
    public async Task Estimate_PostPersistsHeaderAndAdvancesNumbering()
    {
        string db = Guid.NewGuid().ToString();
        using (var seed = new TestMijmsContext(db))
        {
            seed.VoucherTypes.Add(new VoucherType { DocumentType = "Estimate", DocNbrPrefix = "E-", DocNbrLength = 5, LastUsedNumber = 209 });
            await seed.SaveChangesAsync();
        }

        using (var context = new TestMijmsContext(db))
            await NewEstimateController(context).Post(new EstimateHeader { CustGkey = 3 });

        using var verify = new TestMijmsContext(db);
        Assert.Multiple(() =>
        {
            Assert.That(verify.EstimateHeaders.Single().EstNbr, Is.EqualTo("E-00210"));
            Assert.That(verify.VoucherTypes.Single().LastUsedNumber, Is.EqualTo(210));
        });
    }

    [Test]
    public async Task Estimate_PutAndDelete_ArePersisted()
    {
        string db = Guid.NewGuid().ToString();
        int gkey;
        using (var seed = new TestMijmsContext(db))
        {
            var header = new EstimateHeader { EstNbr = "E-00001", CustGkey = 1 };
            seed.EstimateHeaders.Add(header);
            await seed.SaveChangesAsync();
            gkey = header.Gkey;
        }

        using (var context = new TestMijmsContext(db))
            await NewEstimateController(context).Put("E-00001", new EstimateHeader { Gkey = gkey, CustGkey = 2 });

        using (var verify = new TestMijmsContext(db))
            Assert.That(verify.EstimateHeaders.Single().CustGkey, Is.EqualTo(2));

        using (var context = new TestMijmsContext(db))
            await NewEstimateController(context).Delete("E-00001");

        using (var verify = new TestMijmsContext(db))
            Assert.That(verify.EstimateHeaders.Count(), Is.Zero);
    }

    [Test]
    public async Task EstimateLine_PostPutDelete_ArePersisted()
    {
        string db = Guid.NewGuid().ToString();

        int gkey;
        using (var context = new TestMijmsContext(db))
        {
            var created = await NewEstimateLineController(context).Post(new EstimateLine { EstLineNbr = 1, EstimateHdrGkey = 7, ProductGkey = 5 });
            gkey = created.Gkey;
        }

        using (var verify = new TestMijmsContext(db))
            Assert.That(verify.EstimateLines.Single().EstimateHdrGkey, Is.EqualTo(7));

        using (var context = new TestMijmsContext(db))
            await NewEstimateLineController(context).Put(3, new EstimateLine { Gkey = gkey, EstimateHdrGkey = 7, ProductGkey = 8 });

        using (var verify = new TestMijmsContext(db))
        {
            var updated = verify.EstimateLines.Single();
            Assert.That(updated.EstLineNbr, Is.EqualTo(3));
            Assert.That(updated.ProductGkey, Is.EqualTo(8));
        }

        using (var context = new TestMijmsContext(db))
            await NewEstimateLineController(context).Delete(3);

        using (var verify = new TestMijmsContext(db))
            Assert.That(verify.EstimateLines.Count(), Is.Zero);
    }

    [Test]
    public async Task MtblReference_PostAndPut_ArePersisted()
    {
        string db = Guid.NewGuid().ToString();

        int gkey;
        using (var context = new TestMijmsContext(db))
        {
            var created = await NewMtblReferenceController(context).Post(new MtblReference { RefName = "PAYMENT_MODE", RefCode = "UPI", RefValue = "UPI" });
            gkey = created.Gkey;
        }

        using (var context = new TestMijmsContext(db))
            await NewMtblReferenceController(context).Put(new MtblReference { Gkey = gkey, RefName = "PAYMENT_MODE", RefCode = "UPI", RefValue = "UPI / GPay" });

        using var verify = new TestMijmsContext(db);
        Assert.That(new MtblReferenceController(new RepositoryBase<MtblReference>(verify), new UnitOfWork(verify))
                        .Get("PAYMENT_MODE", "UPI")?.RefValue,
                    Is.EqualTo("UPI / GPay"));
    }

    [Test]
    public async Task VoucherType_PostPutDelete_ArePersisted()
    {
        string db = Guid.NewGuid().ToString();

        int gkey;
        using (var context = new TestMijmsContext(db))
        {
            var created = await NewVoucherTypeController(context).Post(new VoucherType { DocumentType = "Cash", DocNbrPrefix = "C-", LastUsedNumber = 0 });
            gkey = created.Gkey;
        }

        using (var context = new TestMijmsContext(db))
            await NewVoucherTypeController(context).Put(new VoucherType { Gkey = gkey, DocumentType = "Cash", DocNbrPrefix = "CSH-", LastUsedNumber = 4 });

        using (var verify = new TestMijmsContext(db))
        {
            var updated = verify.VoucherTypes.Single();
            Assert.That(updated.DocNbrPrefix, Is.EqualTo("CSH-"));
            Assert.That(updated.LastUsedNumber, Is.EqualTo(4));
        }

        using (var context = new TestMijmsContext(db))
            await NewVoucherTypeController(context).Delete("Cash");

        using (var verify = new TestMijmsContext(db))
            Assert.That(verify.VoucherTypes.Count(), Is.Zero);
    }

    private static InvoiceLineController NewInvoiceLineController(MijmsContext context) =>
        new(new RepositoryBase<InvoiceLine>(context), new UnitOfWork(context));

    private static EstimateController NewEstimateController(MijmsContext context) =>
        new(new RepositoryBase<EstimateHeader>(context),
            new RepositoryBase<VoucherType>(context),
            null!, // the workflow is only used by the "save" endpoint
            new UnitOfWork(context));

    private static EstimateLineController NewEstimateLineController(MijmsContext context) =>
        new(new RepositoryBase<EstimateLine>(context), new UnitOfWork(context));

    private static MtblReferenceController NewMtblReferenceController(MijmsContext context) =>
        new(new RepositoryBase<MtblReference>(context), new UnitOfWork(context));

    private static VoucherTypeController NewVoucherTypeController(MijmsContext context) =>
        new(new RepositoryBase<VoucherType>(context), new UnitOfWork(context));

    private sealed class TestMijmsContext(string databaseName) : MijmsContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseInMemoryDatabase(databaseName);
    }
}
