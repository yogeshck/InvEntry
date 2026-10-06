namespace InvEntry.Contracts.StockAdjustments;

public static class StockAdjustmentTypes
{
    public const string Increase = "INCREASE";
    public const string Decrease = "DECREASE";
    public const string Reallocation = "REALLOCATION";
}

public static class StockAdjustmentDirections
{
    public const string In = "IN";
    public const string Out = "OUT";
}

public static class StockAdjustmentStockLevels
{
    public const string Consolidated = "CONSOLIDATED";
    public const string Sku = "SKU";
}

public static class StockAdjustmentMovementKinds
{
    public const string Weight = "WEIGHT";
    public const string WholeItem = "WHOLE_ITEM";
}

public static class StockAdjustmentStatuses
{
    public const string Draft = "DRAFT";
    public const string Posted = "POSTED";
}
