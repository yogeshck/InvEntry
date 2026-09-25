using InvEntry.Models;
using InvEntry.Services;

namespace InvEntry.Test;

[TestFixture]
public sealed class InvoiceProductLookupTests
{
    [Test]
    public async Task Bracelet_CategoryLookup_DoesNotUseCategoryStockCollection()
    {
        var stock = new StubStockService();
        var products = new StubProductViewService(new ProductView { Id = "BRACELET", Category = "BRACELET" });
        var result = await new InvoiceProductLookupService(stock, products).LookupAsync("BRACELET");

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Stock, Is.Null);
            Assert.That(stock.ExactRequests, Is.EqualTo(new[] { "BRACELET" }));
            Assert.That(stock.CategoryRequests, Is.Empty);
            Assert.That(products.Requests, Is.EqualTo(new[] { "BRACELET" }));
        });
    }

    [Test]
    public async Task ValidSku_UsesExactStockAndThenItsCategoryProduct()
    {
        var stock = new StubStockService(new ProductStock
        {
            ProductSku = "BR-0001", Category = "BRACELET", StockQty = 1, IsProductSold = false
        });
        var products = new StubProductViewService(new ProductView { Id = "BRACELET", Category = "BRACELET" });
        var result = await new InvoiceProductLookupService(stock, products).LookupAsync("BR-0001");

        Assert.Multiple(() =>
        {
            Assert.That(result.Stock?.ProductSku, Is.EqualTo("BR-0001"));
            Assert.That(result.Product?.Category, Is.EqualTo("BRACELET"));
            Assert.That(products.Requests, Is.EqualTo(new[] { "BRACELET" }));
        });
    }

    [Test]
    public async Task UnknownOrMissingResponse_ReturnsNotFoundWithoutException()
    {
        var result = await new InvoiceProductLookupService(
            new StubStockService(), new StubProductViewService()).LookupAsync("UNKNOWN");
        var empty = await new InvoiceProductLookupService(
            new StubStockService(), new StubProductViewService()).LookupAsync("  ");

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.Stock, Is.Null);
            Assert.That(empty.Found, Is.False);
        });
    }

    [Test]
    public async Task SoldSku_IsUnavailableAndDoesNotFallBackToCategory()
    {
        var stock = new StubStockService(new ProductStock
        {
            ProductSku = "BR-0002", Category = "BRACELET", StockQty = 0,
            IsProductSold = true, Status = "Sold"
        });
        var products = new StubProductViewService(new ProductView { Id = "BRACELET" });
        var result = await new InvoiceProductLookupService(stock, products).LookupAsync("BR-0002");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsUnavailableSku, Is.True);
            Assert.That(result.Product, Is.Null);
            Assert.That(products.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task CategoryWithMultipleTaggedItems_DoesNotSelectArbitraryStock()
    {
        var stock = new StubStockService(
            exact: null,
            categoryItems:
            [
                new ProductStock { ProductSku = "BR-0001", Category = "BRACELET" },
                new ProductStock { ProductSku = "BR-0002", Category = "BRACELET" }
            ]);
        var products = new StubProductViewService(new ProductView { Id = "BRACELET", Category = "BRACELET" });
        var result = await new InvoiceProductLookupService(stock, products).LookupAsync("BRACELET");

        Assert.Multiple(() =>
        {
            Assert.That(result.Stock, Is.Null);
            Assert.That(stock.CategoryRequests, Is.Empty);
            Assert.That(result.Product, Is.Not.Null);
        });
    }

    private sealed class StubStockService(
        ProductStock? exact = null,
        IEnumerable<ProductStock>? categoryItems = null) : IProductStockService
    {
        public List<string> ExactRequests { get; } = [];
        public List<string> CategoryRequests { get; } = [];
        public Task<ProductStock?> GetExactProductStock(string productSku)
        {
            ExactRequests.Add(productSku);
            return Task.FromResult(exact);
        }
        public Task<IEnumerable<ProductStock>> GetCategoryList(string category)
        {
            CategoryRequests.Add(category);
            return Task.FromResult(categoryItems ?? []);
        }
        public Task<ProductStock> GetProductStock(int gKey) => throw new NotSupportedException();
        public Task<ProductStock> GetProduct(string productId) => throw new NotSupportedException();
        public Task<ProductStock> GetProductStock(string productId) => throw new AssertionException("Legacy ambiguous endpoint must not be called by invoice lookup.");
        public Task<IEnumerable<ProductStock>> GetPendingByGrnLineSummary(int value) => throw new NotSupportedException();
        public Task<ProductStock> ReserveProductSku(int gKey) => throw new NotSupportedException();
        public Task CreateProductStock(ProductStock value) => throw new NotSupportedException();
        public Task UpdateProductStock(ProductStock value) => throw new NotSupportedException();
    }

    private sealed class StubProductViewService(params ProductView[] products) : IProductViewService
    {
        public List<string> Requests { get; } = [];
        public Task<ProductView?> GetOptionalProduct(string productId)
        {
            Requests.Add(productId);
            return Task.FromResult(products.FirstOrDefault(x =>
                string.Equals(x.Id, productId, StringComparison.OrdinalIgnoreCase)));
        }
        public Task<ProductView> GetProduct(string productId) => throw new NotSupportedException();
        public Task<ProductView> GetByProductSku(string productSku) => throw new NotSupportedException();
        public Task<ProductView> GetByCategory(string category) => throw new NotSupportedException();
        public Task<IEnumerable<ProductView>> GetAll() => throw new NotSupportedException();
    }
}