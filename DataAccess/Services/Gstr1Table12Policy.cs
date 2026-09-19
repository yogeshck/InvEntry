namespace DataAccess.Services;

public enum Gstr1Table12SupplyClass
{
    Excluded = 0,
    B2B = 1,
    B2C = 2
}

public sealed class Gstr1Table12Policy
{
    public Gstr1Table12SupplyClass Resolve(
        string? returnCategory,
        bool isRecipientRegistered)
    {
        // Keep GST policy decisions isolated here.
        // Later this resolver can read versioned/effective-date
        // configuration without changing aggregation code.

        if (isRecipientRegistered)
            return Gstr1Table12SupplyClass.B2B;

        if (string.IsNullOrWhiteSpace(returnCategory))
            return Gstr1Table12SupplyClass.Excluded;

        return returnCategory.Trim().ToUpperInvariant() switch
        {
            "B2CS" => Gstr1Table12SupplyClass.B2C,
            "B2CL" => Gstr1Table12SupplyClass.B2C,

            // Do not guess treatment of other categories.
            // Add them only after their Table-12 treatment
            // is explicitly defined in policy/configuration.
            _ => Gstr1Table12SupplyClass.Excluded
        };
    }
}