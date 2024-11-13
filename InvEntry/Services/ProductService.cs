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

        Task CreatProduct(Product product);

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
            return await _mijmsApiService.Get<Product>($"api/productstock/{productId}");
        }

        public async Task CreatProduct(Product product)
        {
            await _mijmsApiService.Post($"api/productstock/", product);
        }

        public async Task UpdateProduct(Product product)
        {
            await _mijmsApiService.Put($"api/productstock/{product.Id}", product);
        }
    }
}
