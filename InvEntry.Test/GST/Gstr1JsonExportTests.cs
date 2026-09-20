using System.Text.Json;
using DataAccess.Controllers;
using DataAccess.Services;
using InvEntry.Contracts.Gst;
using InvEntry.Contracts.Gst.Export;
using Microsoft.AspNetCore.Mvc;

namespace InvEntry.Tests.GST;

[TestFixture]
public sealed class Gstr1JsonExportTests
{
    [TestCase("202609", "092026")]
    [TestCase("202601", "012026")]
    [TestCase("202612", "122026")]
    public void FilingPeriodUsesMonthThenYear(string source, string expected) =>
        Assert.That(Gstr1GstnExportMapper.ToFilingPeriod(source), Is.EqualTo(expected));

    [TestCase(null)]
    [TestCase("")]
    [TestCase("202613")]
    [TestCase("202600")]
    [TestCase("20269")]
    [TestCase("202609 ")]
    [TestCase("20A609")]
    [TestCase("000009")]
    public void InvalidPeriodIsRejected(string? source) =>
        Assert.Throws<ArgumentException>(() => Gstr1GstnExportMapper.ToFilingPeriod(source!));

    [Test]
    public void MapsBothB2csAllocationsWithoutChangingValues()
    {
        var mapped = Gstr1GstnExportMapper.Map(Prepared());
        Assert.Multiple(() =>
        {
            Assert.That(mapped.FilingPeriod, Is.EqualTo("092026"));
            Assert.That(mapped.B2cs.Select(x => x.SupplyType), Is.EqualTo(new[] { "INTRA", "INTER" }));
            Assert.That(mapped.B2cs.Select(x => x.PlaceOfSupplyCode), Is.EqualTo(new[] { "32", "33" }));
            Assert.That(mapped.B2cs.All(x => x.Type == "OE" && x.GstRate == 3M), Is.True);
            Assert.That(mapped.B2cs.Sum(x => x.TaxableValue), Is.EqualTo(7129698.93M));
            Assert.That(mapped.B2cs.Sum(x => x.CgstAmount), Is.EqualTo(105249.96M));
            Assert.That(mapped.B2cs.Sum(x => x.SgstAmount), Is.EqualTo(105249.96M));
            Assert.That(mapped.B2cs.Sum(x => x.IgstAmount), Is.EqualTo(3391M));
            Assert.That(mapped.B2cs.Sum(x => x.CessAmount), Is.Zero);
            Assert.That(mapped.Hsn.B2C.Single().Description, Is.EqualTo("Existing description"));
            Assert.That(mapped.Hsn.B2C.Single().Quantity, Is.EqualTo(436.090M));
        });
    }

    [TestCase(1, 1, 1)]
    [TestCase(0, 0, 0)]
    [TestCase(-1, 1, 0)]
    [TestCase(0, 0, -1)]
    public void ContradictoryOrIndeterminateAllocationIsRejected(int cgst, int sgst, int igst)
    {
        var prepared = Prepared();
        var row = prepared.B2cs.Rows[0];
        row.CgstAmount = cgst;
        row.SgstAmount = sgst;
        row.IgstAmount = igst;
        Assert.Throws<InvalidOperationException>(() => Gstr1GstnExportMapper.Map(prepared));
    }

    [Test]
    public void ValidationErrorsBlockMapping()
    {
        var prepared = Prepared();
        prepared.Validation.Issues.Add(new() { Severity = "Error", Code = "GST-TEST" });
        Assert.Throws<InvalidOperationException>(() => Gstr1GstnExportMapper.Map(prepared));
    }

    [Test]
    public void UnsupportedDimensionsAreNotSilentlyDropped()
    {
        var prepared = Prepared();
        prepared.B2cs.Rows[0].Type = "E";
        Assert.Throws<InvalidOperationException>(() => Gstr1GstnExportMapper.Map(prepared));
        prepared = Prepared();
        prepared.Hsn.B2B.Add(new());
        Assert.Throws<InvalidOperationException>(() => Gstr1GstnExportMapper.Map(prepared));
        prepared = Prepared();
        prepared.DocumentsIssued.Series[0].DocumentType = "Credit Note";
        Assert.Throws<InvalidOperationException>(() => Gstr1GstnExportMapper.Map(prepared));
    }

    [Test]
    public void JsonUsesOnlyGstnPropertyNamesAndNumericAmounts()
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(
            Gstr1GstnExportMapper.Map(Prepared()), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = json.RootElement;
        AssertNames(root, "gstin", "fp", "b2b", "b2cs", "hsn", "doc_issue");
        Assert.That(root.GetProperty("gstin").GetString(), Is.EqualTo("32AGGPR0021E1Z4"));
        var b2cs = root.GetProperty("b2cs")[0];
        AssertNames(b2cs, "pos", "sply_ty", "rt", "txval", "iamt", "camt", "samt", "csamt", "typ");
        foreach (var name in new[] { "rt", "txval", "iamt", "camt", "samt", "csamt" })
            Assert.That(b2cs.GetProperty(name).ValueKind, Is.EqualTo(JsonValueKind.Number));
        var hsn = root.GetProperty("hsn");
        AssertNames(hsn, "hsn_b2c");
        AssertNames(hsn.GetProperty("hsn_b2c")[0],
            "num", "hsn_sc", "desc", "uqc", "qty", "rt", "txval", "iamt", "camt", "samt", "csamt");
        Assert.That(hsn.GetProperty("hsn_b2c")[0].GetProperty("qty").GetDecimal(), Is.EqualTo(436.090M));
        var issued = root.GetProperty("doc_issue");
        AssertNames(issued, "doc_det");
        var details = issued.GetProperty("doc_det");
        Assert.That(details.GetArrayLength(), Is.EqualTo(12));
        AssertNames(details[0], "doc_num", "docs");
        var series = details[0].GetProperty("docs")[0];
        AssertNames(series, "num", "from", "to", "totnum", "cancel", "net_issue");
        Assert.That(series.GetProperty("num").GetInt32(), Is.EqualTo(1));
        Assert.That(series.GetProperty("net_issue").GetInt32(), Is.EqualTo(43));
        for (var i = 0; i < 12; i++)
        {
            Assert.That(details[i].GetProperty("doc_num").GetInt32(), Is.EqualTo(i + 1));
            if (i > 0) Assert.That(details[i].GetProperty("docs").GetArrayLength(), Is.Zero);
        }
    }

    [Test]
    public void NullDescriptionIsOmittedLocally()
    {
        var prepared = Prepared();
        prepared.Hsn.B2C[0].Description = null;
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(Gstr1GstnExportMapper.Map(prepared)));
        Assert.That(json.RootElement.GetProperty("hsn").GetProperty("hsn_b2c")[0]
            .TryGetProperty("desc", out _), Is.False);
    }

    [Test]
    public void NumberingIsDeterministic()
    {
        var prepared = Prepared();
        prepared.Hsn.B2C.Add(new() { HsnCode = "1000", Uqc = "GMS", GstRate = 5M });
        prepared.DocumentsIssued.Series.Add(new()
        {
            DocumentType = "Sales Invoice", Series = "A-", FromNumber = "A-1", ToNumber = "A-2",
            TotalIssued = 2
        });
        var mapped = Gstr1GstnExportMapper.Map(prepared);
        Assert.That(mapped.Hsn.B2C.Select(x => (x.Number, x.HsnCode)),
            Is.EqualTo(new[] { (1, "1000"), (2, "7110") }));
        Assert.That(mapped.DocumentsIssued.Details[0].Series.Select(x => (x.Number, x.FromNumber)),
            Is.EqualTo(new[] { (1, "A-1"), (2, "D-01009") }));
    }

    [Test]
    public async Task ServiceAwaitsPreparationAndPassesCancellation()
    {
        var completion = new TaskCompletionSource<Gstr1ExportPreparationResponse>();
        using var cancellation = new CancellationTokenSource();
        var stub = new PreparationStub { Result = completion.Task };
        var task = new Gstr1JsonExportService(stub).ExportAsync("32AGGPR0021E1Z4", "202609", cancellation.Token);
        Assert.That(task.IsCompleted, Is.False);
        Assert.That(stub.Calls, Is.EqualTo(1));
        Assert.That(stub.Token, Is.EqualTo(cancellation.Token));
        completion.SetResult(Prepared());
        Assert.That((await task).B2cs.Count, Is.EqualTo(2));
    }

    [Test]
    public void InvalidPeriodDoesNotInvokePreparation()
    {
        var stub = new PreparationStub();
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await new Gstr1JsonExportService(stub).ExportAsync("32AGGPR0021E1Z4", "202613"));
        Assert.That(stub.Calls, Is.Zero);
    }

    [Test]
    public async Task EndpointReturns400WhenPreparationBlocksExport()
    {
        var stub = new PreparationStub
        {
            Result = Task.FromException<Gstr1ExportPreparationResponse>(
                new InvalidOperationException("Export blocked: validation returned 1 error(s)."))
        };
        var controller = new Gstr1Controller(null!, null!, null!, null!, null!, null!, null!, null!,
            null!, new Gstr1JsonExportService(stub));
        var result = await controller.GetExportJson("32AGGPR0021E1Z4", "202609", default);
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        var body = JsonSerializer.Serialize(((BadRequestObjectResult)result.Result!).Value);
        Assert.That(body, Does.Contain("validation returned 1 error"));
        Assert.That(body, Does.Contain("/api/gstr1/validation"));
    }

    private static void AssertNames(JsonElement element, params string[] names) =>
        Assert.That(element.EnumerateObject().Select(x => x.Name), Is.EquivalentTo(names));

    private sealed class PreparationStub : IGstr1ExportPreparationService
    {
        public Task<Gstr1ExportPreparationResponse> Result { get; init; } = Task.FromResult(Prepared());
        public int Calls { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<Gstr1ExportPreparationResponse> PrepareAsync(
            string supplierGstin, string returnPeriod, CancellationToken cancellationToken = default)
        {
            Calls++;
            Token = cancellationToken;
            Assert.That(supplierGstin, Is.EqualTo("32AGGPR0021E1Z4"));
            Assert.That(returnPeriod, Is.EqualTo("202609"));
            return Result;
        }
    }

    private static Gstr1ExportPreparationResponse Prepared() => new()
    {
        SupplierGstin = "32AGGPR0021E1Z4",
        ReturnPeriod = "202609",
        B2cs = new()
        {
            Rows =
            [
                new() { Type = "OE", PlaceOfSupplyCode = "32", GstRate = 3M, TaxableValue = 7016665.43M,
                    CgstAmount = 105249.96M, SgstAmount = 105249.96M },
                new() { Type = "OE", PlaceOfSupplyCode = "33", GstRate = 3M, TaxableValue = 113033.50M,
                    IgstAmount = 3391M }
            ]
        },
        Hsn = new()
        {
            B2C = [new() { SupplyClass = "B2C", HsnCode = "7110", Description = "Existing description",
                Uqc = "GMS", GstRate = 3M, TotalQuantity = 436.090M, TaxableValue = 7129698.93M,
                CgstAmount = 105249.96M, SgstAmount = 105249.96M, IgstAmount = 3391M }]
        },
        DocumentsIssued = new()
        {
            Series = [new() { DocumentType = "Sales Invoice", Series = "D-", FromNumber = "D-01009",
                ToNumber = "D-01051", TotalIssued = 43, Cancelled = 0 }]
        }
    };
}