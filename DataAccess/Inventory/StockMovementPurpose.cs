namespace DataAccess.Inventory.ProductStock;

public enum StockMovementPurpose
{
    Sale,

    PurchaseReceipt,

    StockAdjustmentIncrease,
    StockAdjustmentDecrease,

    // Linked item/category material reallocation. These are deliberately
    // separate from adjustment IN/OUT so a transfer does not affect the
    // legacy AdjustedQty / AdjustedWeight totals.
    StockReallocationOut,
    StockReallocationIn,

    MaterialIssue,
    MaterialReceipt,

    BranchTransferOut,
    BranchTransferIn,

    WorkshopIssue,
    WorkshopReceipt,

    OldMetalPurchase,
    OldMetalTransferOut
}
