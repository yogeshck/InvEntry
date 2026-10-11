using DataAccess.Models;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Net;
using System.Text;

namespace DataAccess.Tests;

public sealed class FinanceSyncServiceTests
{
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.Conflict)]
    public void Permanent_validation_failures_are_business_errors(
        HttpStatusCode statusCode)
    {
        Assert.That(FinanceSyncService.ClassifyHttpFailure(statusCode),
            Is.EqualTo(FinanceSyncService.BusinessError));
    }

    [Test]
    public async Task Timeout_marks_posting_outcome_uncertain()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var item = CreateItem(FinanceSyncService.Pending);
        using var client = new HttpClient(new Handler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }))
        {
            BaseAddress = new Uri("https://finance.test"),
            Timeout = TimeSpan.FromMilliseconds(25)
        };

        var sent = await service.DispatchClaimedAsync(
            item, client, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.False);
            Assert.That(item.Status, Is.EqualTo(FinanceSyncService.Uncertain));
            Assert.That(item.LastError, Does.Contain("outcome is uncertain"));
        });
    }

    [Test]
    public async Task Uncertain_event_already_posted_is_reconciled_without_posting_again()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var item = CreateItem(FinanceSyncService.Uncertain);
        var methods = new List<HttpMethod>();
        using var client = new HttpClient(new Handler((request, _) =>
        {
            methods.Add(request.Method);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"posted\":true,\"documentGkey\":91}",
                    Encoding.UTF8, "application/json")
            });
        })) { BaseAddress = new Uri("https://finance.test") };

        var sent = await service.DispatchClaimedAsync(
            item, client, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(sent, Is.True);
            Assert.That(item.Status, Is.EqualTo(FinanceSyncService.Sent));
            Assert.That(methods, Is.EqualTo(new[] { HttpMethod.Get }));
        });
    }

    [Test]
    public async Task Server_failure_is_retryable()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var item = CreateItem(FinanceSyncService.Pending);
        using var client = new HttpClient(new Handler((_, _) =>
            Task.FromResult(new HttpResponseMessage(
                HttpStatusCode.ServiceUnavailable))))
        { BaseAddress = new Uri("https://finance.test") };

        await service.DispatchClaimedAsync(item, client, CancellationToken.None);

        Assert.That(item.Status, Is.EqualTo(FinanceSyncService.RetryableError));
    }

    private static MijmsContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MijmsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MijmsContext(options);
    }

    private static FinanceSyncService CreateService(MijmsContext context) =>
        new(context, new FinanceInvoicePayloadBuilder(context),
            new Factory(new HttpClient()));

    private static FinanceSyncOutbox CreateItem(string status) => new()
    {
        EventType = "FINANCIAL_DOCUMENT",
        SourceType = "INVOICE",
        SourceEventId = "INVOICE-7-FINAL",
        Payload = "{\"orgGkey\":1,\"sourceSystem\":\"INVENTRY\",\"sourceEventId\":\"INVOICE-7-FINAL\",\"movements\":[{\"direction\":\"IN\",\"paymentGroup\":\"CASH\",\"amount\":1}]}",
        Status = status,
        CreatedOn = DateTime.Now
    };

    private sealed class Factory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class Handler(
        Func<HttpRequestMessage, CancellationToken,
            Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
