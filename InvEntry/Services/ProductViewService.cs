using DevExpress.XtraLayout.Customization;
using InvEntry.Models;
using InvEntry.Utils.Options;
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

        Task<ProductView> GetByProductSku(string productSku);

        Task<IEnumerable<ProductView>> GetByCategory(string category);

        Task<IEnumerable<ProductView>> GetAll();
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
            return await _mijmsApiService.Get<ProductView>($"api/ProductView/{productId}");
        }

        public async Task<ProductView> GetByProductSku(string productSku)
        {
            return await _mijmsApiService.Get<ProductView>($"api/ProductView/productSku/{productSku}");
        }

        public async Task<IEnumerable<ProductView>> GetByCategory(string category)
        {
            return await _mijmsApiService.GetEnumerable<ProductView>($"api/ProductView/category/{category}");
        }

        public async Task<IEnumerable<ProductView>> GetAll()
        {
            return await _mijmsApiService.GetEnumerable<ProductView>($"api/ProductView/");
        }
    }
}
