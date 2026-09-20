using System.Text.Json;
using DataAccess.Services;
using InvEntry.Contracts.Gst;
using InvEntry.Contracts.Gst.Export;

namespace InvEntry.Tests.GST;

[TestFixture]
public sealed class Gstr1B2bJsonExportTests
{
    [Test]
    public void ZeroB2bMapsToEmptyArray()
    {
        var mapped = Gstr1GstnExportMapper.Map(Prepared());
        Assert.That(mapped.B2b, Is.Empty);
        using var json = Serialize(mapped);
        Assert.That(json.RootElement.GetProperty("b2b").GetArrayLength(), Is.Zero);
    }

    [Test]
    public void MapsOneRecipientAndInvoice()
    {
        var recipient = Gstr1GstnExportMapper.Map(Prepared(Invoice())).B2b.Single();
        Assert.Multiple(() =>
        {
            Assert.That(recipient.RecipientGstin, Is.EqualTo("32ABCDE1234F1Z5"));
            Assert.That(recipient.Invoices.Single().InvoiceNumber, Is.EqualTo("INV-1"));
            Assert.That(recipient.Invoices.Single().InvoiceValue, Is.EqualTo(118M));
            Assert.That(recipient.Invoices.Single().PlaceOfSupplyCode, Is.EqualTo("32"));
        });
    }

    [Test]
    public void GroupsMultipleInvoicesForSameRecipient()
    {
        var mapped = Gstr1GstnExportMapper.Map(Prepared(
            Invoice(number: "INV-2", date: new(2026, 9, 2), gkey: 2),
            Invoice(number: "INV-1", date: new(2026, 9, 1), gkey: 1)));
        Assert.That(mapped.B2b, Has.Count.EqualTo(1));
        Assert.That(mapped.B2b[0].Invoices.Select(x => x.InvoiceNumber),
            Is.EqualTo(new[] { "INV-1", "INV-2" }));
    }

    [Test]
    public void DifferentRecipientsProduceOrderedGroups()
    {
        var mapped = Gstr1GstnExportMapper.Map(Prepared(
            Invoice(recipient: "33ABCDE1234F1Z5"),
            Invoice(recipient: "29ABCDE1234F1Z5", number: "INV-2", gkey: 2)));
        Assert.That(mapped.B2b.Select(x => x.RecipientGstin),
            Is.EqualTo(new[] { "29ABCDE1234F1Z5", "33ABCDE1234F1Z5" }));
    }

    [Test]
    public void MultipleLinesHaveDeterministicItemNumbers()
    {
        var invoice = Invoice(lines:
        [
            Line(2, 12M, 40M),
            Line(1, 5M, 60M)
        ]);
        var items = Gstr1GstnExportMapper.Map(Prepared(invoice)).B2b[0].Invoices[0].Items;
        Assert.That(items.Select(x => (x.Number, x.ItemDetail.GstRate)),
            Is.EqualTo(new[] { (1, 5M), (2, 12M) }));
    }

    [Test]
    public void IntraStateAmountsMapUnchanged()
    {
        var detail = ItemDetail(Line(1, 18M, 100M, cgst: 9M, sgst: 9M));
        Assert.Multiple(() =>
        {
            Assert.That(detail.CgstAmount, Is.EqualTo(9M));
            Assert.That(detail.SgstAmount, Is.EqualTo(9M));
            Assert.That(detail.IgstAmount, Is.Zero);
        });
    }

    [Test]
    public void InterStateAmountMapsUnchanged()
    {
        var detail = ItemDetail(Line(1, 18M, 100M, igst: 18M));
        Assert.Multiple(() =>
        {
            Assert.That(detail.IgstAmount, Is.EqualTo(18M));
            Assert.That(detail.CgstAmount, Is.Zero);
            Assert.That(detail.SgstAmount, Is.Zero);
        });
    }

    [Test]
    public void ReverseChargeFalseMapsToN() =>
        Assert.That(MapInvoice(Invoice(reverseCharge: false)).ReverseCharge, Is.EqualTo("N"));

    [Test]
    public void ReverseChargeTrueMapsToY() =>
        Assert.That(MapInvoice(Invoice(reverseCharge: true)).ReverseCharge, Is.EqualTo("Y"));

    [Test]
    public void OrdinaryB2bMapsToRegularInvoiceType() =>
        Assert.That(MapInvoice(Invoice()).InvoiceType, Is.EqualTo("R"));

    [Test]
    public void SezB2bIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Gstr1GstnExportMapper.Map(Prepared(Invoice(isSez: true))));
        Assert.That(exception!.Message, Does.Contain("unsupported special B2B"));
    }

    [Test]
    public void DeemedExportB2bIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Gstr1GstnExportMapper.Map(Prepared(Invoice(isDeemedExport: true))));
        Assert.That(exception!.Message, Does.Contain("unsupported special B2B"));
    }

    [Test]
    public void SezAndDeemedExportB2bIsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Gstr1GstnExportMapper.Map(Prepared(Invoice(isSez: true, isDeemedExport: true))));
        Assert.That(exception!.Message, Does.Contain("unsupported special B2B"));
    }
    [Test]
    public void InvoiceDateUsesDayMonthYear() =>
        Assert.That(MapInvoice(Invoice(date: new(2026, 9, 3))).InvoiceDate,
            Is.EqualTo("03-09-2026"));

    [Test]
    public void B2bUsesExactGstnPropertyNames()
    {
        using var json = Serialize(Gstr1GstnExportMapper.Map(Prepared(Invoice())));
        var recipient = json.RootElement.GetProperty("b2b")[0];
        AssertNames(recipient, "ctin", "inv");
        var invoice = recipient.GetProperty("inv")[0];
        AssertNames(invoice, "inum", "idt", "val", "pos", "rchrg", "inv_typ", "itms");
        var item = invoice.GetProperty("itms")[0];
        AssertNames(item, "num", "itm_det");
        AssertNames(item.GetProperty("itm_det"), "rt", "txval", "iamt", "camt", "samt", "csamt");
    }

    private static Gstr1GstnB2bInvoice MapInvoice(Gstr1B2bInvoiceResponse invoice) =>
        Gstr1GstnExportMapper.Map(Prepared(invoice)).B2b.Single().Invoices.Single();

    private static Gstr1GstnB2bItemDetail ItemDetail(Gstr1B2bLineResponse line) =>
        MapInvoice(Invoice(lines: [line])).Items.Single().ItemDetail;

    private static JsonDocument Serialize(Gstr1GstnExportResponse response) =>
        JsonDocument.Parse(JsonSerializer.Serialize(response));

    private static void AssertNames(JsonElement element, params string[] names) =>
        Assert.That(element.EnumerateObject().Select(x => x.Name), Is.EquivalentTo(names));

    private static Gstr1ExportPreparationResponse Prepared(
        params Gstr1B2bInvoiceResponse[] invoices) => new()
    {
        SupplierGstin = "32AGGPR0021E1Z4",
        ReturnPeriod = "202609",
        B2b = new() { Invoices = invoices.ToList() }
    };

    private static Gstr1B2bInvoiceResponse Invoice(
        string recipient = "32ABCDE1234F1Z5", string number = "INV-1",
        DateOnly? date = null, long gkey = 1, bool reverseCharge = false,
        bool isSez = false, bool isDeemedExport = false,
        List<Gstr1B2bLineResponse>? lines = null) => new()
    {
        DocumentGkey = gkey,
        RecipientGstin = recipient,
        DocumentNumber = number,
        DocumentDate = date ?? new DateOnly(2026, 9, 1),
        PlaceOfSupplyCode = "32",
        InvoiceValue = 118M,
        ReverseCharge = reverseCharge,
        IsSez = isSez,
        IsDeemedExport = isDeemedExport,
        Lines = lines ?? [Line(1, 18M, 100M, cgst: 9M, sgst: 9M)]
    };

    private static Gstr1B2bLineResponse Line(
        int number, decimal rate, decimal taxable, decimal igst = 0M,
        decimal cgst = 0M, decimal sgst = 0M, decimal cess = 0M) => new()
    {
        LineNumber = number,
        GstRate = rate,
        TaxableValue = taxable,
        IgstAmount = igst,
        CgstAmount = cgst,
        SgstAmount = sgst,
        CessAmount = cess
    };
}
