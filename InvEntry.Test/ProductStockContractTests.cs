using System.Text.Json;
using System.Reflection;
using InvEntry.Models;
using InvEntry.ViewModels;

namespace InvEntry.Test;

[TestFixture]
public class ProductStockContractTests
{
    [Test]
    public void TemporaryStockFactory_CreatesOneLinkedPendingRowPerSuppliedUnit()
    {
        var lines = new[]
        {
            new GrnLineSummary
            {
                GKey = 501,
                ProductGkey = 6,
                ProductCategory = "MALA",
                SuppliedQty = 2
            },
            new GrnLineSummary
            {
                GKey = 502,
                ProductGkey = 11,
                ProductCategory = "SILVER",
                SuppliedQty = 1
            }
        };
        var summaryKeys = new Dictionary<string, int>
        {
            ["MALA"] = 20,
            ["SILVER"] = 21
        };

        var stocks = BuildTemporaryStocks(lines, summaryKeys, "SUP-1");

        Assert.Multiple(() =>
        {
            Assert.That(stocks, Has.Count.EqualTo(3));
            Assert.That(stocks.Count(x => x.GrnLineSummaryGkey == 501), Is.EqualTo(2));
            Assert.That(stocks.Count(x => x.GrnLineSummaryGkey == 502), Is.EqualTo(1));
            Assert.That(stocks, Has.All.Matches<ProductStock>(x =>
                x.Status == "Pending Tag" &&
                x.IsBarcodePrinted == false &&
                x.IsProductSold == false &&
                x.ProductSku?.StartsWith("TMP-", StringComparison.Ordinal) == true &&
                x.SuppliedQty == 1 &&
                x.StockQty == 1));
        });
    }

    [Test]
    public void JsonRoundTrip_PreservesStockAndGrnLineSummaryKeys()
    {
        var source = new ProductStock
        {
            GKey = 42,
            StockSummaryGkey = 100,
            GrnLineSummaryGkey = 200
        };

        string json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<ProductStock>(json);

        Assert.That(restored, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(restored!.GKey, Is.EqualTo(42));
            Assert.That(restored.StockSummaryGkey, Is.EqualTo(100));
            Assert.That(restored.GrnLineSummaryGkey, Is.EqualTo(200));
        });
    }
    [Test]
    public void ReloadedWeighingRow_RetainsStableStockIdentityWithoutSerializingIt()
    {
        var pendingStock = new ProductStock
        {
            GKey = 175,
            GrnLineSummaryGkey = 25,
            ProductSku = "GBL2-0175",
            Status = "Pending Tag"
        };
        var reloadedRow = new GrnLine
        {
            GrnLineSumryGkey = 25,
            ProductStockGkey = pendingStock.GKey,
            ProductSku = pendingStock.ProductSku
        };

        Assert.Multiple(() =>
        {
            Assert.That(reloadedRow.ProductStockGkey, Is.EqualTo(pendingStock.GKey));
            Assert.That(pendingStock.GrnLineSummaryGkey, Is.EqualTo(reloadedRow.GrnLineSumryGkey));
            Assert.That(JsonSerializer.Serialize(reloadedRow),
                Does.Not.Contain(nameof(GrnLine.ProductStockGkey)));
        });
    }

    [Test]
    public void RefreshedWeighingRow_IsAcceptedForItsPendingStock()
    {
        var pendingStock = new ProductStock
        {
            GKey = 175,
            GrnLineSummaryGkey = 25,
            ProductSku = "GBL2-0175",
            Status = "Pending Tag",
            IsBarcodePrinted = false,
            IsProductSold = false
        };
        var refreshedRow = new GrnLine
        {
            GrnLineSumryGkey = 25,
            ProductStockGkey = pendingStock.GKey,
            ProductSku = pendingStock.ProductSku
        };

        Assert.That(GetPendingStockRejectionReason(pendingStock, refreshedRow), Is.Null);
    }

    [Test]
    public void PreviouslyFinalizedStock_ReportsItsExactStatusAfterRefresh()
    {
        var finalizedStock = new ProductStock
        {
            GKey = 2607,
            GrnLineSummaryGkey = 120,
            ProductSku = "GBL2-0175",
            Status = "In-Stock",
            IsBarcodePrinted = true,
            IsProductSold = false
        };
        var staleRow = new GrnLine
        {
            GrnLineSumryGkey = 120,
            ProductStockGkey = finalizedStock.GKey,
            ProductSku = finalizedStock.ProductSku
        };

        Assert.That(
            GetPendingStockRejectionReason(finalizedStock, staleRow),
            Is.EqualTo("ProductStock GKEY 2607 is not pending tag; its status is 'In-Stock'."));
    }

    private static string? GetPendingStockRejectionReason(
        ProductStock? productStock,
        GrnLine line)
    {
        var method = typeof(ProductStockEntryViewModel).GetMethod(
            "GetPendingStockRejectionReason",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        return (string?)method!.Invoke(null, new object?[]
        {
            productStock,
            line,
            line.ProductStockGkey.GetValueOrDefault()
        });
    }

    private static IReadOnlyList<ProductStock> BuildTemporaryStocks(
        IEnumerable<GrnLineSummary> lines,
        IReadOnlyDictionary<string, int> summaryKeys,
        string supplierId)
    {
        var method = typeof(GRNViewModel).GetMethod(
            "BuildTemporaryStockItems",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        return (IReadOnlyList<ProductStock>)method!.Invoke(
            null,
            new object[] { lines, summaryKeys, supplierId })!;
    }
}
