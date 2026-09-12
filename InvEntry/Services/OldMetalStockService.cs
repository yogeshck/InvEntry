using InvEntry.Contracts.OldMetal;
using System;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IOldMetalStockService
{ 

    Task<OldMetalStockResponse?>
        GetCurrentStockAsync(
            int productGkey);
}

public sealed class OldMetalStockService
    : IOldMetalStockService
{
    private readonly IMijmsApiService
        _mijmsApiService;


    public OldMetalStockService(
        IMijmsApiService mijmsApiService)
    {
        _mijmsApiService =
            mijmsApiService;
    }


    public async Task<OldMetalStockResponse?>
        GetCurrentStockAsync(
            int productGkey)
    {
        if (productGkey <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productGkey));
        }


        return await _mijmsApiService
            .GetResponse<OldMetalStockResponse>(
                $"api/old-metal-stock/product/{productGkey}");
    }
}