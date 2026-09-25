using System.Linq.Expressions;
using DataAccess.Inventory.ProductStock;
using DataAccess.Models;
using DataAccess.Repository;

namespace InvEntry.Test;

[TestFixture]
public sealed class InvoiceStockMovementTests
{
    [Test]
    public void SkuSale_ReducesTaggedAndCategoryStock_AndPostsBothTransactions()
    {
        var fixture = CreateFixture();

        fixture.Service.PostMovements([Sale(1, "RING-0001")]);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Stock.StockQty, Is.Zero);
            Assert.That(fixture.Stock.IsProductSold, Is.True);
            Assert.That(fixture.Summary.StockQty, Is.EqualTo(4));
            Assert.That(fixture.Summary.GrossWeight, Is.EqualTo(40m));
            Assert.That(fixture.Summary.StoneWeight, Is.EqualTo(4m));
            Assert.That(fixture.Summary.NetWeight, Is.EqualTo(36m));
            Assert.That(fixture.Summary.BalanceWeight, Is.EqualTo(36m));
            Assert.That(fixture.Transactions.Items, Has.Count.EqualTo(1));
            Assert.That(fixture.SummaryTransactions.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void LegacySale_ReducesOnlyCategoryStock()
    {
        var fixture = CreateFixture();

        fixture.Service.PostMovements([Sale(1, null)]);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Stock.StockQty, Is.EqualTo(1));
            Assert.That(fixture.Summary.StockQty, Is.EqualTo(4));
            Assert.That(fixture.Transactions.Items, Is.Empty);
            Assert.That(fixture.SummaryTransactions.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void MixedAndRepeatedCategoryLines_ApplyOneSummaryMovementPerLine()
    {
        var fixture = CreateFixture();
        fixture.Service.PostMovements([
            Sale(1, "RING-0001"),
            Sale(2, null),
            Sale(3, null)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Summary.StockQty, Is.EqualTo(2));
            Assert.That(fixture.Summary.BalanceWeight, Is.EqualTo(18m));
            Assert.That(fixture.SummaryTransactions.Items, Has.Count.EqualTo(3));
            Assert.That(fixture.Transactions.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void InvalidOrSoldSku_BlocksBeforeAnySummaryMutation()
    {
        var invalid = CreateFixture();
        Assert.Throws<InvalidOperationException>(() =>
            invalid.Service.PostMovements([Sale(1, "MISSING")]));
        Assert.That(invalid.Summary.StockQty, Is.EqualTo(5));

        var sold = CreateFixture();
        sold.Stock.IsProductSold = true;
        Assert.Throws<InvalidOperationException>(() =>
            sold.Service.PostMovements([Sale(1, "RING-0001")]));
        Assert.That(sold.Summary.StockQty, Is.EqualTo(5));
    }

    [Test]
    public void InsufficientCumulativeCategoryStock_AllowsNegativeInvoiceBalance()
    {
        var fixture = CreateFixture();
        fixture.Summary.StockQty = 1;

        fixture.Service.PostMovements([Sale(1, null), Sale(2, null)]);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Summary.StockQty, Is.EqualTo(-1));
            Assert.That(fixture.SummaryTransactions.Items, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void PreviouslyPostedInvoiceLine_IsNotDeductedAgain()
    {
        var fixture = CreateFixture();
        fixture.SummaryTransactions.Items.Add(new ProductTransactionSummary
        {
            RefLineGkey = 1,
            DocumentType = "SALE_INVOICE"
        });

        Assert.Throws<InvalidOperationException>(() =>
            fixture.Service.PostMovements([Sale(1, null)]));
        Assert.That(fixture.Summary.StockQty, Is.EqualTo(5));
    }

    private static StockMovementRequest Sale(int lineGkey, string? sku) => new()
    {
        DocumentGkey = 100,
        DocumentLineGkey = lineGkey,
        DocumentNumber = "INV-100",
        DocumentDate = new DateTime(2026, 9, 25),
        DocumentType = "SALE_INVOICE",
        ProductGkey = 10,
        ProductSku = sku ?? string.Empty,
        ProductCategory = "RING",
        Direction = StockMovementDirection.Out,
        Purpose = StockMovementPurpose.Sale,
        Quantity = 1,
        GrossWeight = 10m,
        StoneWeight = 1m,
        NetWeight = 9m
    };

    private static Fixture CreateFixture()
    {
        var stock = new ProductStock
        {
            Gkey = 20, ProductGkey = 10, ProductSku = "RING-0001",
            StockQty = 1, GrossWeight = 10m, StoneWeight = 1m,
            NetWeight = 9m, BalanceWeight = 10m, IsProductSold = false
        };
        var summary = new ProductStockSummary
        {
            Gkey = 30, ProductGkey = 10, Category = "RING",
            StockQty = 5, GrossWeight = 50m, StoneWeight = 5m,
            NetWeight = 45m, BalanceWeight = 45m
        };
        var stocks = new FakeRepository<ProductStock>(stock);
        var summaries = new FakeRepository<ProductStockSummary>(summary);
        var transactions = new FakeRepository<ProductTransaction>();
        var summaryTransactions = new FakeRepository<ProductTransactionSummary>();
        var service = new StockMovementService(
            stocks, summaries, transactions, summaryTransactions, new MijmsContext());
        return new Fixture(service, stock, summary, transactions, summaryTransactions);
    }

    private sealed record Fixture(
        StockMovementService Service,
        ProductStock Stock,
        ProductStockSummary Summary,
        FakeRepository<ProductTransaction> Transactions,
        FakeRepository<ProductTransactionSummary> SummaryTransactions);

    private sealed class FakeRepository<T>(params T[] initial) : IRepositoryBase<T> where T : class
    {
        public List<T> Items { get; } = [.. initial];
        public void Add(T value) => Items.Add(value);
        public void AddRange(IEnumerable<T> values) => Items.AddRange(values);
        public void Update(T value) { }
        public void BulkUpdate(IEnumerable<T> values) { }
        public void Remove(T value) => Items.Remove(value);
        public T? Get(Expression<Func<T, bool>> predicate) => Items.AsQueryable().FirstOrDefault(predicate);
        public IEnumerable<T> GetList(Expression<Func<T, bool>> predicate) => Items.AsQueryable().Where(predicate).ToList();
        public IEnumerable<T> GetAll() => Items;
        public int Count() => Items.Count;
        public T? GetId(int id) => throw new NotSupportedException();
        public Task<T?> GetIdAsync(int id) => throw new NotSupportedException();
        public Task<T?> GetAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Get(predicate));
        public Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(GetList(predicate));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult(GetAll());
        public Task<int> CountAsync() => Task.FromResult(Count());
        public void Dispose() { }
    }
}