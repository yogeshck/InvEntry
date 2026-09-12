using InvEntry.Contracts.StockTransfers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IStockTransferService
    {
        Task<StockTransferDetailResponse> CreateAsync(
            CreateStockTransferRequest request);
    }

    public sealed class StockTransferService
    : IStockTransferService
    {
        private readonly IMijmsApiService
            _mijmsApiService;


        public StockTransferService(
            IMijmsApiService mijmsApiService)
        {
            _mijmsApiService =
                mijmsApiService
                ?? throw new ArgumentNullException(
                    nameof(mijmsApiService));
        }


        public async Task<StockTransferDetailResponse>
            CreateAsync(
                CreateStockTransferRequest request)
        {
            ArgumentNullException.ThrowIfNull(
                request);


            return await _mijmsApiService
                .Post<
                    CreateStockTransferRequest,
                    StockTransferDetailResponse>(
                        "api/stock-transfers",
                        request);
        }
    }
}
