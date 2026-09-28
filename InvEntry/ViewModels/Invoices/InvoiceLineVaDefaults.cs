namespace InvEntry.ViewModels.Invoices;

public static class InvoiceLineVaDefaults
{
    public static decimal ResolvePercent(
        decimal? productStockPercent,
        decimal? productPercent) =>
        productStockPercent
        ?? productPercent
        ?? 0M;

    public static decimal PreserveOrZero(decimal? persistedValue) =>
        persistedValue ?? 0M;
}
