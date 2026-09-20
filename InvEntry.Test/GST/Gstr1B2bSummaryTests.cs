using DataAccess.Services;
using static DataAccess.Services.Gstr1B2bSummaryService;

namespace InvEntry.Tests.GST;

[TestFixture]
public sealed class Gstr1B2bSummaryTests
{
    [Test]
    public void EmptyPeriodReturnsValidZeroSummary()
    {
        var result = Prepare([]);
        Assert.Multiple(() =>
        {
            Assert.That(result.InvoiceCount, Is.Zero);
            Assert.That(result.Invoices, Is.Empty);
            Assert.That(result.TotalInvoiceValue, Is.Zero);
            Assert.That(result.TotalTaxableValue, Is.Zero);
            Assert.That(result.TotalIgst, Is.Zero);
            Assert.That(result.TotalCgst, Is.Zero);
            Assert.That(result.TotalSgst, Is.Zero);
            Assert.That(result.TotalCess, Is.Zero);
        });
    }

    [Test]
    public void OneInvoiceMapsAuthoritativeHeaderValues()
    {
        var invoice = Prepare([Document()]).Invoices.Single();
        Assert.Multiple(() =>
        {
            Assert.That(invoice.DocumentGkey, Is.EqualTo(10));
            Assert.That(invoice.SourceGkey, Is.EqualTo(20));
            Assert.That(invoice.RecipientGstin, Is.EqualTo("32ABCDE1234F1Z5"));
            Assert.That(invoice.DocumentNumber, Is.EqualTo("INV-1"));
            Assert.That(invoice.InvoiceValue, Is.EqualTo(118M));
            Assert.That(invoice.ReverseCharge, Is.True);
        });
    }

    [Test]
    public void MultipleRatesRemainSeparate()
    {
        var source = Document(lines:
        [
            Line(2, 12M, 40M, cgst: 2.4M, sgst: 2.4M),
            Line(1, 5M, 60M, cgst: 1.5M, sgst: 1.5M)
        ], taxable: 100M, cgst: 3.9M, sgst: 3.9M);
        Assert.That(Prepare([source]).Invoices.Single().Lines.Select(x => x.GstRate),
            Is.EqualTo(new[] { 5M, 12M }));
    }

    [Test]
    public void IntraStateAmountsArePreserved()
    {
        var result = Prepare([Document()]);
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCgst, Is.EqualTo(9M));
            Assert.That(result.TotalSgst, Is.EqualTo(9M));
            Assert.That(result.TotalIgst, Is.Zero);
        });
    }

    [Test]
    public void InterStateAmountIsPreserved()
    {
        var source = Document(lines: [Line(1, 18M, 100M, igst: 18M)],
            taxable: 100M, cgst: 0M, sgst: 0M, igst: 18M);
        var result = Prepare([source]);
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalIgst, Is.EqualTo(18M));
            Assert.That(result.TotalCgst, Is.Zero);
            Assert.That(result.TotalSgst, Is.Zero);
        });
    }

    [Test]
    public void ReconciliationMismatchIsRejected()
    {
        Assert.That(() => Prepare([Document(taxable: 100.02M)]),
            Throws.InvalidOperationException.With.Message.Contains("do not reconcile"));
    }

    [Test]
    public void InvoiceAndLineOrderingIsDeterministic()
    {
        var later = Document(gkey: 20, number: "INV-2", date: new(2026, 9, 2));
        var second = Document(gkey: 30, number: "INV-3", date: new(2026, 9, 1));
        var first = Document(gkey: 10, number: "INV-1", date: new(2026, 9, 1), lines:
        [
            Line(2, 12M, 40M, cgst: 2.4M, sgst: 2.4M, gkey: 3),
            Line(1, 5M, 60M, cgst: 1.5M, sgst: 1.5M, gkey: 2)
        ], taxable: 100M, cgst: 3.9M, sgst: 3.9M);

        var result = Prepare([later, second, first]);
        Assert.That(result.Invoices.Select(x => x.DocumentGkey), Is.EqualTo(new long[] { 10, 30, 20 }));
        Assert.That(result.Invoices[0].Lines.Select(x => x.LineNumber), Is.EqualTo(new[] { 1, 2 }));
    }

    private static InvEntry.Contracts.Gst.Gstr1B2bSummaryResponse Prepare(
        IReadOnlyCollection<B2bSourceDocument> documents) =>
        Gstr1B2bSummaryService.Prepare("32AGGPR0021E1Z4", "202609", documents);

    private static B2bSourceDocument Document(
        long gkey = 10, string number = "INV-1", DateOnly? date = null,
        List<B2bSourceLine>? lines = null, decimal taxable = 100M,
        decimal cgst = 9M, decimal sgst = 9M, decimal igst = 0M) => new()
    {
        DocumentGkey = gkey,
        SourceGkey = 20,
        RecipientGstin = " 32abcde1234f1z5 ",
        DocumentNumber = number,
        DocumentDate = date ?? new DateOnly(2026, 9, 1),
        PlaceOfSupplyCode = "32",
        InvoiceValue = 118M,
        SupplyType = "INTRA",
        TaxType = "CGST_SGST",
        ReverseCharge = true,
        TaxableValue = taxable,
        CgstAmount = cgst,
        SgstAmount = sgst,
        IgstAmount = igst,
        Lines = lines ?? [Line(1, 18M, 100M, cgst: 9M, sgst: 9M)]
    };

    private static B2bSourceLine Line(
        int number, decimal rate, decimal taxable, decimal cgst = 0M,
        decimal sgst = 0M, decimal igst = 0M, long gkey = 1) => new()
    {
        LineGkey = gkey,
        LineNumber = number,
        GstRate = rate,
        TaxableValue = taxable,
        CgstAmount = cgst,
        SgstAmount = sgst,
        IgstAmount = igst
    };
}
