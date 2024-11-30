using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IProductService
    {
        Task<Product> GetProduct(string productId);

        Task<Product> GetByCategory(string category);

        Task CreateProduct(Product product);

        Task UpdateProduct(Product product);
    }

    public class ProductService : IProductService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public ProductService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<Product> GetProduct(string productId)
        {
            return await _mijmsApiService.Get<Product>($"api/product/{productId}");
        }

        public async Task<Product> GetByCategory(string category)
        {
            return await _mijmsApiService.Get<Product>($"api/product/{category}");
        }

        public async Task CreateProduct(Product product)
        {
            await _mijmsApiService.Post($"api/product/", product);
        }

        public async Task UpdateProduct(Product product)
        {
            await _mijmsApiService.Put($"api/product/{product.Id}", product);
        }
    }
}
