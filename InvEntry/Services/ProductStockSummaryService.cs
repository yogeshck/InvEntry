using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{

    public interface IProductStockSummaryService
    {

        Task<ProductStockSummary> GetByProductGkey(int? productGkey);

        Task<ProductStockSummary> GetProductStockSummary(string productId);
 
        Task<ProductStockSummary> GetProductStockSummaryByProductSku(string productSku);
  
        Task<ProductStockSummary> GetProductStockSummaryByCategory(string category);
  
        Task CreateProductStockSummary(ProductStockSummary productStockSummary);
   
        Task UpdateProductStockSummary(ProductStockSummary productStockSummary);
        
    }
 
    public class ProductStockSummaryService : IProductStockSummaryService
    {
            
        private readonly IMijmsApiService _mijmsApiService;
 
        public ProductStockSummaryService(IMijmsApiService mijmsApiService)
        {  
            _mijmsApiService = mijmsApiService;
        }

        public async Task<ProductStockSummary> GetByProductGkey(int? productGkey)
        {
            return await _mijmsApiService.Get<ProductStockSummary>($"api/productStockSummary/productGkey/{productGkey}");
        }

        public async Task<ProductStockSummary> GetProductStockSummary(string productId)
        {
            return await _mijmsApiService.Get<ProductStockSummary>($"api/productStockSummary/{productId}");
        }

        public async Task<ProductStockSummary> GetProductStockSummaryByProductSku(string productSku)
        {
            return await _mijmsApiService.Get<ProductStockSummary>($"api/productStockSummary/productSku/{productSku}");
        }

        public async Task<ProductStockSummary> GetProductStockSummaryByCategory(string category)
        {
            return await _mijmsApiService.Get<ProductStockSummary>($"api/productStockSummary/category/{category}");
        }

        public async Task CreateProductStockSummary(ProductStockSummary productStockSummary)
        {
            await _mijmsApiService.Post($"api/productStockSummary/", productStockSummary);
        }

        public async Task UpdateProductStockSummary(ProductStockSummary productStockSummary)
        {
            await _mijmsApiService.Put($"api/productStockSummary/{productStockSummary.ProductGkey}", productStockSummary);
        }
    
    }

}
