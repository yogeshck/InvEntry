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
        Task<ProductStock> GetProductStock(string productId);

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

        public async Task<ProductStock> GetProductStock(string productId)
        {
            return await _mijmsApiService.Get<ProductStock>($"api/productstock/stock/{productId}");
        }

        public async Task CreateProductStock(ProductStock productStock)
        {

            await _mijmsApiService.Post($"api/productstock/", productStock);
        }

        public async Task UpdateProductStock(ProductStock productStock)
        {
            await _mijmsApiService.Put($"api/productstock/{productStock.ProductGkey}", productStock);
        }
    }
}
