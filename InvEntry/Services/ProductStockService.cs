using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IProductStockService
    {
        Task<ProductStock> GetProductStock(int gKey);
        Task<ProductStock> GetProduct(string productId);
        Task<ProductStock> GetProductStock(string productId);
        Task<ProductStock?> GetExactProductStock(string productSku);
        Task<IEnumerable<ProductStock>> GetCategoryList(string category);
        Task<IEnumerable<ProductStock>> GetPendingByGrnLineSummary(int grnLineSummaryGkey);
        Task<ProductStock> ReserveProductSku(int gKey);
        Task CreateProductStock(ProductStock productStock);

        Task UpdateProductStock(ProductStock product);
    }

    public class ProductStockService : IProductStockService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public ProductStockService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<ProductStock> GetProductStock(int gKey)
        {
            return await _mijmsApiService.Get<ProductStock>($"api/productstock/key/{gKey}");
        }

        public async Task<ProductStock> GetProduct(string productId)
        {
            return await _mijmsApiService.Get<ProductStock>($"api/productstock/{productId}");
        }

        public async Task<ProductStock> GetProductStock(string productId)
        {
            return await _mijmsApiService.Get<ProductStock>($"api/productstock/stock/{productId}");
        }

        public async Task<ProductStock?> GetExactProductStock(string productSku)
        {
            return await _mijmsApiService.GetOptional<ProductStock>(
                $"api/productstock/exact/{Uri.EscapeDataString(productSku)}");
        }
        public async Task<IEnumerable<ProductStock>> GetCategoryList(string category)
        {
            return await _mijmsApiService.GetEnumerable<ProductStock>($"api/productstock/category/{category}");
        }

        public async Task<IEnumerable<ProductStock>> GetPendingByGrnLineSummary(int grnLineSummaryGkey)
        {
            return await _mijmsApiService.GetEnumerable<ProductStock>(
                $"api/productstock/pending/grn-line-summary/{grnLineSummaryGkey}");
        }

        public async Task<ProductStock> ReserveProductSku(int gKey)
        {
            return await _mijmsApiService.PostResponse<ProductStock>(
                $"api/productstock/{gKey}/reserve-sku");
        }

        public async Task CreateProductStock(ProductStock productStock)
        {

            await _mijmsApiService.Post($"api/productstock/", productStock);
        }

        public async Task UpdateProductStock(ProductStock productStock)
        {
            await _mijmsApiService.Put($"api/productstock/{productStock.GKey}", productStock);
        }

/*        public async Task DeleteProductStock(ProductStock productStock)
        {
            await _mijmsApiService.($"api/productstock/{productStock.ProductGkey}");
        }*/
    }
}
