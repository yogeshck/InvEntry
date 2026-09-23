using System.Text.Json;
using System.Reflection;
using InvEntry.Models;
using InvEntry.ViewModels;

namespace InvEntry.Test;

[TestFixture]
public class ProductStockContractTests
{
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
    }}
