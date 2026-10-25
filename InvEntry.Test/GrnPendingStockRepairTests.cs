using System.Reflection;
using InvEntry.Models;
using InvEntry.Services;

namespace InvEntry.Test;

[TestFixture]
public class GrnPendingStockRepairTests
{
    [Test]
    public async Task MissingItems_PostsReceiptOnceAndCreatesOnlyMissingPendingItems()
    {
        var fixture = CreateFixture(stockSummaryGkey: 42);
        var header = Header();
        var line = Line(suppliedQty: 3);

        int created = await fixture.Service.RepairAsync(header, line, existingItemCount: 1);

        Assert.That(created, Is.EqualTo(2));

        Assert.That(fixture.Movements, Has.Count.EqualTo(1));
        var movement = fixture.Movements[0];
        Assert.Multiple(() =>
        {
            // Must match the request GRN save posts, so the movement API's
            // idempotency check recognises an already-posted receipt.
            Assert.That(movement.DocumentType, Is.EqualTo("GRN"));
            Assert.That(movement.TransactionType, Is.EqualTo("Receipt"));
            Assert.That(movement.RefGkey, Is.EqualTo(header.GKey));
            Assert.That(movement.RefLineGkey, Is.EqualTo(line.GKey));
            Assert.That(movement.Direction, Is.EqualTo(StockMovementDirection.In));
            Assert.That(movement.Quantity, Is.EqualTo(3));
            Assert.That(movement.NetWeight, Is.EqualTo(line.NetWeight));
            Assert.That(movement.DocumentNbr, Is.EqualTo("GRN-0103"));
        });

        Assert.That(fixture.CreatedStocks, Has.Count.EqualTo(2));
        Assert.That(fixture.CreatedStocks, Has.All.Matches<ProductStock>(stock =>
            stock.Status == "Pending Tag" &&
            stock.IsBarcodePrinted == false &&
            stock.IsProductSold == false &&
            stock.GrnLineSummaryGkey == line.GKey &&
            stock.StockSummaryGkey == 42 &&
            stock.ProductGkey == line.ProductGkey &&
            stock.SupplierId == "SUPP1" &&
            stock.ProductSku!.StartsWith("TMP-")));
        Assert.That(fixture.CreatedStocks.Select(stock => stock.ProductSku).Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task NothingMissing_DoesNothing()
    {
        var fixture = CreateFixture(stockSummaryGkey: 42);

        int created = await fixture.Service.RepairAsync(Header(), Line(suppliedQty: 2), existingItemCount: 2);

        Assert.That(created, Is.Zero);
        Assert.That(fixture.Movements, Is.Empty);
        Assert.That(fixture.CreatedStocks, Is.Empty);
    }

    [Test]
    public void StockSummaryNotFound_ThrowsWithoutCreatingItems()
    {
        var fixture = CreateFixture(stockSummaryGkey: null);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.RepairAsync(Header(), Line(suppliedQty: 2), existingItemCount: 0));

        Assert.That(fixture.CreatedStocks, Is.Empty);
    }

    [Test]
    public void UnsavedLine_IsRejected()
    {
        var fixture = CreateFixture(stockSummaryGkey: 42);
        var line = Line(suppliedQty: 2);
        line.GKey = 0;

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.RepairAsync(Header(), line, existingItemCount: 0));

        Assert.That(fixture.Movements, Is.Empty);
        Assert.That(fixture.CreatedStocks, Is.Empty);
    }

    private static GrnHeader Header() => new()
    {
        GKey = 148,
        GrnNbr = "GRN-0103",
        GrnDate = new DateTime(2026, 9, 26),
        SupplierId = "SUPP1"
    };

    private static GrnLineSummary Line(int suppliedQty) => new()
    {
        GKey = 194,
        GrnHdrGkey = 148,
        ProductGkey = 2,
        ProductCategory = "BANGLE",
        SuppliedQty = suppliedQty,
        GrossWeight = 120.500m,
        StoneWeight = 0.500m,
        NetWeight = 120.000m
    };

    private static Fixture CreateFixture(int? stockSummaryGkey)
    {
        var movements = new List<StockMovementRequest>();
        var createdStocks = new List<ProductStock>();

        var movementService = Proxy<IProductStockMovementService>((method, args) =>
        {
            if (method.Name == nameof(IProductStockMovementService.ApplyAsync))
                movements.Add((StockMovementRequest)args![0]!);
            return DefaultReturn(method.ReturnType);
        });

        var stockSummaryService = Proxy<IProductStockSummaryService>((method, _) =>
            method.Name == nameof(IProductStockSummaryService.GetProductStockSummaryByCategory)
                ? Task.FromResult<ProductStockSummary>(
                    stockSummaryGkey is int gkey ? new ProductStockSummary { GKey = gkey } : null!)
                : DefaultReturn(method.ReturnType));

        var productStockService = Proxy<IProductStockService>((method, args) =>
        {
            if (method.Name == nameof(IProductStockService.CreateProductStock))
                createdStocks.Add((ProductStock)args![0]!);
            return DefaultReturn(method.ReturnType);
        });

        return new Fixture(
            new GrnPendingStockRepairService(movementService, stockSummaryService, productStockService),
            movements,
            createdStocks);
    }

    private sealed record Fixture(
        GrnPendingStockRepairService Service,
        List<StockMovementRequest> Movements,
        List<ProductStock> CreatedStocks);

    private static T Proxy<T>(
        Func<MethodInfo, object?[]?, object?>? handler = null)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, ServiceProxy<T>>();
        ((ServiceProxy<T>)(object)proxy).Handler = handler;
        return proxy;
    }

    private class ServiceProxy<T> : DispatchProxy where T : class
    {
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler?.Invoke(targetMethod!, args) ?? DefaultReturn(targetMethod!.ReturnType);
    }

    private static object? DefaultReturn(Type returnType)
    {
        if (returnType == typeof(Task))
            return Task.CompletedTask;

        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            Type resultType = returnType.GetGenericArguments()[0];
            object? result = resultType.IsValueType
                ? Activator.CreateInstance(resultType)
                : null;
            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [result]);
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }
}
