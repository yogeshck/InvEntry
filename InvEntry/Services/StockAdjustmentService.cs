using InvEntry.Contracts.StockAdjustments;
using System;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IStockAdjustmentService
{
    Task<StockAdjustmentResponse> CreateAsync(
        CreateStockAdjustmentRequest request);

    Task<StockAdjustmentResponse> GetDetailAsync(
        int gkey);
}

public sealed class StockAdjustmentService
    : IStockAdjustmentService
{
    private readonly IMijmsApiService _mijmsApiService;

    public StockAdjustmentService(
        IMijmsApiService mijmsApiService)
    {
        _mijmsApiService =
            mijmsApiService
            ?? throw new ArgumentNullException(
                nameof(mijmsApiService));
    }

    public Task<StockAdjustmentResponse> CreateAsync(
        CreateStockAdjustmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _mijmsApiService
            .Post<
                CreateStockAdjustmentRequest,
                StockAdjustmentResponse>(
                    "api/stock-adjustments",
                    request);
    }

    public Task<StockAdjustmentResponse> GetDetailAsync(
        int gkey)
    {
        if (gkey <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gkey),
                "Stock adjustment Gkey must be greater than zero.");
        }

        return _mijmsApiService
            .GetResponse<StockAdjustmentResponse>(
                $"api/stock-adjustments/{gkey}");
    }
}
