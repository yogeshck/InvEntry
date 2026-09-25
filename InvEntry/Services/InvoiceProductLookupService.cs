using System;
using System.Threading.Tasks;
using InvEntry.Models;

namespace InvEntry.Services;

public sealed record InvoiceProductLookupResult(
    ProductView? Product,
    ProductStock? Stock,
    bool IsUnavailableSku)
{
    public bool Found => Product != null && !IsUnavailableSku;
}

public sealed class InvoiceProductLookupService
{
    private readonly IProductStockService _stockService;
    private readonly IProductViewService _productViewService;

    public InvoiceProductLookupService(
        IProductStockService stockService,
        IProductViewService productViewService)
    {
        _stockService = stockService;
        _productViewService = productViewService;
    }

    public async Task<InvoiceProductLookupResult> LookupAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return new(null, null, false);

        string value = identifier.Trim().ToUpperInvariant();
        var stock = await _stockService.GetExactProductStock(value);

        if (stock != null)
        {
            if (stock.IsProductSold == true ||
                !string.Equals(stock.Status, "In-Stock", StringComparison.OrdinalIgnoreCase) &&
                stock.StockQty.GetValueOrDefault() <= 0)
            {
                return new(null, stock, true);
            }

            var skuProduct = await _productViewService.GetOptionalProduct(
                stock.Category ?? value);
            return new(skuProduct, stock, false);
        }

        var categoryProduct = await _productViewService.GetOptionalProduct(value);
        return new(categoryProduct, null, false);
    }
}