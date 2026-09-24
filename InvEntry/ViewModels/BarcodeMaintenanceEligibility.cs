using InvEntry.Models;
using System;

namespace InvEntry.ViewModels;

public enum BarcodeMaintenanceEligibilityState
{
    ReadyForReprint,
    PendingInitialTagging,
    InvalidStockData,
    OtherBusinessState
}

public sealed record BarcodeMaintenanceEligibility(
    BarcodeMaintenanceEligibilityState State,
    string DisplayText,
    string Guidance,
    bool CanPreviewOrReprint);

public static class BarcodeMaintenanceEligibilityEvaluator
{
    public static BarcodeMaintenanceEligibility Evaluate(ProductStock? stock)
    {
        if (stock is null)
            return new(BarcodeMaintenanceEligibilityState.InvalidStockData, "Invalid Stock Data",
                "Select an existing ProductStock record.", false);

        bool hasGrnAssociation = stock.GrnLineSummaryGkey.GetValueOrDefault() > 0 &&
                                 stock.StockSummaryGkey.GetValueOrDefault() > 0;
        bool pendingInitialTag = string.Equals(stock.Status?.Trim(), "Pending Tag", StringComparison.OrdinalIgnoreCase) &&
                                 hasGrnAssociation && !stock.IsBarcodePrinted;
        if (pendingInitialTag)
        {
            return new(BarcodeMaintenanceEligibilityState.PendingInitialTagging,
                "Pending Initial Tagging",
                "Pending initial weighing and tagging. Complete this item through GRN Weighing & Tagging before using Barcode Maintenance.",
                false);
        }

        bool validWeights = stock.GrossWeight.HasValue && stock.GrossWeight.Value > 0m &&
                            stock.NetWeight.HasValue && stock.NetWeight.Value > 0m &&
                            stock.StoneWeight.HasValue && stock.StoneWeight.Value >= 0m &&
                            stock.StoneWeight.Value <= stock.GrossWeight.Value;
        if (!validWeights)
        {
            return new(BarcodeMaintenanceEligibilityState.InvalidStockData,
                "Invalid Stock Data",
                "Data-integrity warning: required tag weights are missing or invalid. Correct the record through the appropriate existing stock/GRN workflow.",
                false);
        }

        if (stock.GKey > 0 && !string.IsNullOrWhiteSpace(stock.ProductSku) &&
            stock.ProductGkey.GetValueOrDefault() > 0 && !string.IsNullOrWhiteSpace(stock.Category) &&
            stock.IsBarcodePrinted)
        {
            return new(BarcodeMaintenanceEligibilityState.ReadyForReprint,
                "Ready for Reprint", "Ready to preview or reprint this existing SKU.", true);
        }

        return new(BarcodeMaintenanceEligibilityState.OtherBusinessState,
            $"Other State: {stock.Status ?? "Unknown"}",
            "This record is not an initial pending-GRN tag and is not marked as a completed printed tag. Barcode Maintenance cannot preview or reprint it.",
            false);
    }
}
