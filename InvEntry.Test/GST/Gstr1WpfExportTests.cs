using System.Net;
using System.Net.Http;
using System.Text;
using InvEntry.Contracts.Gst;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.ViewModels;

namespace InvEntry.Tests.GST;

[TestFixture]
public class Gstr1WpfExportTests
{
    [Test]
    public async Task DownloadPreservesUtf8BytesAndEscapesScope()
    {
        var original = Encoding.UTF8.GetBytes("{ \"desc\":\"gold — café\", \"txval\":1.00 }\r\n");
        var handler = new Handler(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(original) });
        var service = new Gstr1ReportService(new MijmsApiService(new Factory(handler)));
        var actual = await service.GetExportJsonAsync("32A&B", "202609");
        Assert.That(actual, Is.EqualTo(original));
        Assert.That(handler.Uri!.Query, Does.Contain("supplierGstin=32A%26B"));
        Assert.That(handler.Uri.AbsolutePath, Is.EqualTo("/api/gstr1/export-json"));
    }

    [Test]
    public void BackendExportErrorIsNotReturnedAsAFile()
    {
        var handler = new Handler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"Export blocked: validation returned 1 error\"}")
        });
        var service = new Gstr1ReportService(new MijmsApiService(new Factory(handler)));
        var error = Assert.ThrowsAsync<HttpRequestException>(() => service.GetExportJsonAsync("32TEST", "202609"));
        Assert.That(error!.Message, Does.Contain("Export blocked: validation returned 1 error"));
    }

    [Test]
    public void ScopeChangesInvalidateReadyStatus()
    {
        var vm = new Gstr1ReturnViewModel(new Reports(), new Company(), null!);
        Assert.That(vm.ExportJsonCommand.CanExecute(null), Is.True);
        vm.ReturnMonth = vm.ReturnMonth.AddMonths(-1);
        Assert.That(vm.Validation, Is.Null);
        Assert.That(vm.ExportJsonCommand.CanExecute(null), Is.False);
        vm.LoadReturnCommand.Execute(null);
        Assert.That(vm.ExportJsonCommand.CanExecute(null), Is.True);
        vm.SupplierGstin = "different";
        Assert.That(vm.Validation, Is.Null);
        Assert.That(vm.ExportJsonCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void BusyStateAndMissingValidationDisableExport()
    {
        var vm = new Gstr1ReturnViewModel(new Reports(), new Company(), null!);
        vm.IsBusy = true;
        Assert.That(vm.ExportJsonCommand.CanExecute(null), Is.False);
        vm.IsBusy = false;
        vm.IsExporting = true;
        Assert.That(vm.ExportJsonCommand.CanExecute(null), Is.False);
        vm.IsExporting = false;
        vm.Validation = null;
        Assert.That(vm.ExportJsonCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task ExportRefreshesValidationAndBlocksDuplicateAndNewlyInvalidExports()
    {
        var reports = new Reports();
        var vm = new Gstr1ReturnViewModel(reports, new Company(), null!);
        var completion = new TaskCompletionSource<Gstr1ValidationResponse>();
        reports.PendingValidation = completion.Task;
        var export = vm.ExportJsonCommand.ExecuteAsync(null);
        Assert.That(vm.IsExporting, Is.True);
        Assert.That(vm.ExportJsonCommand.CanExecute(null), Is.False);
        Assert.That(vm.LoadReturnCommand.CanExecute(null), Is.False);
        Assert.That(reports.Downloads, Is.Zero);
        completion.SetResult(new()
        {
            SupplierGstin = vm.SupplierGstin, ReturnPeriod = vm.ReturnPeriod,
            Issues = [new() { Severity = "Error", Message = "Changed since load" }]
        });
        await export;
        Assert.That(reports.Downloads, Is.Zero);
        Assert.That(vm.IsExporting, Is.False);
        Assert.That(vm.ExportStatusMessage, Does.Contain("Export blocked"));
        Assert.That(vm.Validation!.ErrorCount, Is.EqualTo(1));
    }

    [Test]
    public async Task OldScopeLoadCannotRestoreExportReadiness()
    {
        var reports = new Reports();
        var vm = new Gstr1ReturnViewModel(reports, new Company(), null!);
        var oldPeriod = vm.ReturnPeriod;
        var pending = new TaskCompletionSource<Gstr1ValidationResponse>();
        reports.PendingValidation = pending.Task;
        var load = vm.LoadReturnCommand.ExecuteAsync(null);
        vm.ReturnMonth = vm.ReturnMonth.AddMonths(-1);
        pending.SetResult(new() { SupplierGstin = vm.SupplierGstin, ReturnPeriod = oldPeriod });
        await load;
        Assert.That(vm.Validation, Is.Null);
        Assert.That(vm.ExportJsonCommand.CanExecute(null), Is.False);
    }

    private sealed class Company : IOrgThisCompanyViewService
    {
        public Task<OrgThisCompanyView> GetOrgThisCompany() =>
            Task.FromResult(new OrgThisCompanyView { GstNbr = "32TEST", CompanyName = "Test" });
    }

    private sealed class Reports : IGstr1ReportService
    {
        public Task<Gstr1ValidationResponse>? PendingValidation { get; set; }
        public int Downloads { get; private set; }
        public Task<Gstr1ValidationResponse> GetValidationAsync(string gstin, string period) =>
            PendingValidation ?? Task.FromResult(new Gstr1ValidationResponse { SupplierGstin = gstin, ReturnPeriod = period });
        public Task<byte[]> GetExportJsonAsync(string gstin, string period)
        {
            Downloads++;
            return Task.FromResult(Array.Empty<byte>());
        }
        public Task<Gstr1ReturnSummaryResponse> GetSummaryAsync(string gstin, string period) =>
            Task.FromResult(new Gstr1ReturnSummaryResponse { SupplierGstin = gstin, ReturnPeriod = period });
        public Task<IReadOnlyList<Gstr1DocumentResponse>> GetDocumentsAsync(string gstin, string period) =>
            Task.FromResult<IReadOnlyList<Gstr1DocumentResponse>>([]);
        public Task<IReadOnlyList<Gstr1DocumentLineResponse>> GetDocumentLinesAsync(long key, string gstin, string period) =>
            Task.FromResult<IReadOnlyList<Gstr1DocumentLineResponse>>([]);
    }

    private sealed class Handler(HttpResponseMessage response) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Uri = request.RequestUri;
            return Task.FromResult(response);
        }
    }

    private sealed class Factory(Handler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.That(name, Is.EqualTo("mijms"));
            return new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        }
    }
}
