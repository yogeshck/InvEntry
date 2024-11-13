using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IProductViewService
    {
        Task<ProductView> GetProduct(string productId);
    }

    public class ProductViewService : IProductViewService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public ProductViewService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<ProductView> GetProduct(string productId)
        {
            return await _mijmsApiService.Get<ProductView>($"api/productView/{productId}");
        }
    }
}
