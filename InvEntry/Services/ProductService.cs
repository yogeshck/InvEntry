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

        Task<Product> GetByGkey(int productGkey);

        Task<Product> GetByCategory(string category);

        Task<IEnumerable<Product>> GetAll();

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

        public async Task<Product> GetByGkey(int productGkey)
        {
            return await _mijmsApiService.Get<Product>($"api/product/gkey/{productGkey}");
        }

        public async Task<Product> GetByCategory(string category)
        {
            return await _mijmsApiService.Get<Product>($"api/product/{category}");
        }

        public async Task<IEnumerable<Product>> GetAll()
        {
            return await _mijmsApiService.GetEnumerable<Product>("api/product");
        }

        public async Task CreateProduct(Product product)
        {
            await _mijmsApiService.Post($"api/product/", product);
        }

        // The API route takes the numeric GKey, not the product Id.
        public async Task UpdateProduct(Product product)
        {
            await _mijmsApiService.Put($"api/product/{product.GKey}", product);
        }
    }
}
