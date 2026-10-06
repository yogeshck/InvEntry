namespace InvEntry.Contracts.StockAdjustments;

public static class StockAdjustmentTypes
{
    public const string Increase = "INCREASE";
    public const string Decrease = "DECREASE";
    public const string Reallocation = "REALLOCATION";
}

public static class StockAdjustmentReasonCodes
{
    public const string PhysicalExcess = "PHY_EXCESS";
    public const string MissedReceipt = "MISSED_RECEIPT";
    public const string WrongOutCorrection = "WRONG_OUT_CORRECTION";
    public const string WeightCorrectionIn = "WEIGHT_CORRECTION_IN";
    public const string TagRecovery = "TAG_RECOVERY";
    public const string OpeningCorrectionIn = "OPENING_CORRECTION_IN";

    public const string PhysicalShortage = "PHY_SHORTAGE";
    public const string Loss = "LOSS";
    public const string Damage = "DAMAGE";
    public const string WrongInCorrection = "WRONG_IN_CORRECTION";
    public const string WeightCorrectionOut = "WEIGHT_CORRECTION_OUT";
    public const string TagMissing = "TAG_MISSING";
    public const string OpeningCorrectionOut = "OPENING_CORRECTION_OUT";

    public const string ComponentTransfer = "COMPONENT_TRANSFER";
    public const string HookTransfer = "HOOK_TRANSFER";
    public const string StoneTransfer = "STONE_TRANSFER";
    public const string DesignModification = "DESIGN_MODIFICATION";
    public const string CustomerModification = "CUSTOMER_MODIFICATION";
    public const string RepairReallocation = "REPAIR_REALLOCATION";

    public const string Other = "OTHER";

    private static readonly IReadOnlySet<string> IncreaseReasons =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PhysicalExcess,
            MissedReceipt,
            WrongOutCorrection,
            WeightCorrectionIn,
            TagRecovery,
            OpeningCorrectionIn,
            Other
        };

    private static readonly IReadOnlySet<string> DecreaseReasons =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PhysicalShortage,
            Loss,
            Damage,
            WrongInCorrection,
            WeightCorrectionOut,
            TagMissing,
            OpeningCorrectionOut,
            Other
        };

    private static readonly IReadOnlySet<string> ReallocationReasons =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ComponentTransfer,
            HookTransfer,
            StoneTransfer,
            DesignModification,
            CustomerModification,
            RepairReallocation,
            Other
        };

    public static bool IsValidForAdjustmentType(
        string adjustmentType,
        string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(adjustmentType) ||
            string.IsNullOrWhiteSpace(reasonCode))
        {
            return false;
        }

        return adjustmentType.Trim().ToUpperInvariant() switch
        {
            StockAdjustmentTypes.Increase => IncreaseReasons.Contains(reasonCode.Trim()),
            StockAdjustmentTypes.Decrease => DecreaseReasons.Contains(reasonCode.Trim()),
            StockAdjustmentTypes.Reallocation => ReallocationReasons.Contains(reasonCode.Trim()),
            _ => false
        };
    }
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
