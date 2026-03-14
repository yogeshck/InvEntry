using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IStockVerifyScanService
    {
        Task<StockVerifyScan> GetVerifiedStock(string barCode);

        Task CreateVerifiedStock(StockVerifyScan barCode);

        Task CreateVerifiedStockBulk(List<StockVerifyScan> scanBufferLst);

        //Task UpdateVerifiedStock(StockVerifyScan barCode);
    }

    public class StockVerifyScanService : IStockVerifyScanService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public StockVerifyScanService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<StockVerifyScan> GetVerifiedStock(string barCode)
        {
            return await _mijmsApiService.Get<StockVerifyScan>($"api/stockverifyscan/{barCode}");
        }

        public async Task CreateVerifiedStock(StockVerifyScan barCode)
        {

            await _mijmsApiService.Post($"api/stockverifyscan/", barCode);
        }

        public async Task CreateVerifiedStockBulk(List<StockVerifyScan> scanBufferLst)
        {
            var entities = scanBufferLst.ToList();

            await _mijmsApiService.PostList($"api/stockverifyscan/bulk/", scanBufferLst);
        }

/*        public async Task UpdateProductStock(StockVerifyScan barCode)
        {
            await _mijmsApiService.Put($"api/stockverifyscan/{Stockverifyscan.ProductGkey}", barCode);
        }*/
    }
}
