using InvEntry.Models;
using InvEntry.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http;
using System.Text;

namespace InvEntry.Test;

[TestFixture]
public class ApiGkeyLookupTests
{
    [Test]
    public async Task Get_ProductGkey2_DeserializesSuccessfulResponse()
    {
        var service = CreateService(_ => Json(HttpStatusCode.OK,
            "{\"gKey\":2,\"id\":\"BANGLE\",\"description\":\"Golden Bangle 916\",\"category\":\"BANGLE\",\"purity\":\"916\"}"));

        Product product = await service.Get<Product>("api/product/gkey/2");

        Assert.Multiple(() =>
        {
            Assert.That(product.GKey, Is.EqualTo(2));
            Assert.That(product.Id, Is.EqualTo("BANGLE"), "Business ID remains distinct from database GKey.");
            Assert.That(product.Description, Is.EqualTo("Golden Bangle 916"));
            Assert.That(product.Purity, Is.EqualTo("916"));
        });
    }

    [Test]
    public async Task Get_StockSummaryGkey6_DeserializesExactSummary()
    {
        var service = CreateService(request =>
        {
            Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/api/productStockSummary/gkey/6"));
            return Json(HttpStatusCode.OK, "{\"gKey\":6,\"productGkey\":2,\"vaPercent\":15.00}");
        });

        ProductStockSummary summary = await service.Get<ProductStockSummary>("api/productStockSummary/gkey/6");

        Assert.Multiple(() =>
        {
            Assert.That(summary.GKey, Is.EqualTo(6));
            Assert.That(summary.ProductGkey, Is.EqualTo(2));
            Assert.That(summary.VaPercent, Is.EqualTo(15.00m));
        });
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public void Get_NonSuccess_PreservesHttpStatus(HttpStatusCode status)
    {
        var service = CreateService(_ => Json(status, "lookup failed"));

        var error = Assert.ThrowsAsync<HttpRequestException>(() =>
            service.Get<Product>("api/product/gkey/2"));

        Assert.That(error!.StatusCode, Is.EqualTo(status));
    }

    [Test]
    public void Get_ConnectionFailure_IsNotReportedAsMissingRecord()
    {
        var service = CreateService(_ => throw new HttpRequestException("connection refused"));

        var error = Assert.ThrowsAsync<HttpRequestException>(() =>
            service.Get<Product>("api/product/gkey/2"));

        Assert.That(error!.Message, Does.Contain("Unable to reach the mijms API"));
    }

    [Test]
    public void GkeyRoutes_AreExplicitAndClientControllerPathsMatch()
    {
        string root = FindRepositoryRoot();
        string productService = File.ReadAllText(Path.Combine(root, "InvEntry", "Services", "ProductService.cs"));
        string summaryService = File.ReadAllText(Path.Combine(root, "InvEntry", "Services", "ProductStockSummaryService.cs"));
        string productController = File.ReadAllText(Path.Combine(root, "DataAccess", "Controllers", "ProductController.cs"));
        string summaryController = File.ReadAllText(Path.Combine(root, "DataAccess", "Controllers", "ProductStockSummaryController.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(productService, Does.Contain("api/product/gkey/{productGkey}"));
            Assert.That(productController, Does.Contain("[HttpGet(\"gkey/{productGkey:int}\")]"));
            Assert.That(productController, Does.Contain("x.Gkey == productGkey"));
            Assert.That(productController, Does.Not.Contain("x.Id == productGkey"));
            Assert.That(summaryService, Does.Contain("api/productStockSummary/gkey/{stockSummaryGkey}"));
            Assert.That(summaryController, Does.Contain("[HttpGet(\"gkey/{stockSummaryGkey:int}\")]"));
            Assert.That(summaryController, Does.Contain("x.Gkey == stockSummaryGkey"));
        });
    }

    private static MijmsApiService CreateService(Func<HttpRequestMessage, HttpResponseMessage> response) =>
        new(new StubHttpClientFactory(new HttpClient(new StubHandler(response))
        {
            BaseAddress = new Uri("https://localhost:7001/")
        }));

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "InvEntry")) &&
                Directory.Exists(Path.Combine(directory.FullName, "DataAccess")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage result = response(request);
            result.RequestMessage = request;
            return Task.FromResult(result);
        }
    }
}
