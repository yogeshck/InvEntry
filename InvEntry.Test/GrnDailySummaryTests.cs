using System.Reflection;
using InvEntry.Models;
using InvEntry.ViewModels;

namespace InvEntry.Test;

[TestFixture]
public sealed class GrnDailySummaryTests
{
    [Test]
    public void ReceiptArithmetic_AddsStoneWeightToStockInStoneWeight()
    {
        var summary = new ProductTransactionSummary
        {
            StockInQty = 2,
            ClosingQty = 5,
            StockInGrossWeight = 10m,
            StockInStoneWeight = 3m,
            StockInNetWeight = 7m,
            StockOutStoneWeight = 100m,
            ClosingGrossWeight = 20m,
            ClosingStoneWeight = 4m,
            ClosingNetWeight = 16m
        };
        var transaction = new ProductTransaction
        {
            TransactionQty = 1,
            TransactionGrossWeight = 6m,
            TransactionStoneWeight = 2m,
            TransactionNetWeight = 4m
        };

        ApplyReceipt(summary, transaction);

        Assert.Multiple(() =>
        {
            Assert.That(summary.StockInQty, Is.EqualTo(3));
            Assert.That(summary.ClosingQty, Is.EqualTo(6));
            Assert.That(summary.StockInGrossWeight, Is.EqualTo(16m));
            Assert.That(summary.StockInStoneWeight, Is.EqualTo(5m));
            Assert.That(summary.StockInNetWeight, Is.EqualTo(11m));
            Assert.That(summary.ClosingGrossWeight, Is.EqualTo(26m));
            Assert.That(summary.ClosingStoneWeight, Is.EqualTo(6m));
            Assert.That(summary.ClosingNetWeight, Is.EqualTo(20m));
        });
    }

    private static void ApplyReceipt(
        ProductTransactionSummary summary,
        ProductTransaction transaction)
    {
        var method = typeof(GRNViewModel).GetMethod(
            "ApplyReceiptToDailySummary",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        method!.Invoke(null, new object[] { summary, transaction });
    }
}
