namespace DataAccess.Inventory.ProductStock;

public enum StockMovementPurpose
{
    Sale,

    PurchaseReceipt,

    StockAdjustmentIncrease,
    StockAdjustmentDecrease,

    MaterialIssue,
    MaterialReceipt,

    BranchTransferOut,
    BranchTransferIn,

    WorkshopIssue,
    WorkshopReceipt
}