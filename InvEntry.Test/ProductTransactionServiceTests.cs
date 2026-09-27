using InvEntry.Services;
using System.Net;
using System.Net.Http;
using System.Text;

namespace InvEntry.Test;

[TestFixture]
public sealed class ProductTransactionServiceTests
{
    [TestCase("MALA")]
    [TestCase("SILVER")]
    public async Task GetByCategory_MissingRecord_ReturnsNull(string category)
    {
        var service = CreateService(request =>
        {
            Assert.That(
                request.RequestUri!.AbsolutePath,
                Is.EqualTo($"/api/productTransaction/category/{category}"));
            return Response(HttpStatusCode.NotFound, string.Empty);
        });

        var result = await service.GetByCategory(category);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByCategory_EmptySuccessfulResponse_ReturnsNull()
    {
        var service = CreateService(_ => Response(HttpStatusCode.OK, string.Empty));

        var result = await service.GetByCategory("MALA");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetByCategory_ExistingRecord_ReturnsTransaction()
    {
        var service = CreateService(_ => Response(
            HttpStatusCode.OK,
            "{\"gKey\":12,\"productCategory\":\"MALA\",\"closingQty\":4}"));

        var result = await service.GetByCategory("MALA");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GKey, Is.EqualTo(12));
            Assert.That(result.ProductCategory, Is.EqualTo("MALA"));
        });
    }

    private static ProductTransactionService CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> response) =>
        new(new MijmsApiService(new StubHttpClientFactory(
            new HttpClient(new StubHandler(response))
            {
                BaseAddress = new Uri("https://localhost:7001/")
            })));

    private static HttpResponseMessage Response(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var result = response(request);
            result.RequestMessage = request;
            return Task.FromResult(result);
        }
    }
}
