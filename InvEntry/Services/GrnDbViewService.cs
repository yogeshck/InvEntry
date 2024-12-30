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
    public interface IGrnDbViewService
    {
/*        Task<GrnDbView> GetProduct(string productId);

        Task<GrnDbView> GetByProductSku(string productSku);

        Task<IEnumerable<GrnDbView>> GetByCategory(string category);*/

        Task<IEnumerable<GrnDbView>> GetAll();

        Task<IEnumerable<GrnDbView>> GetAll(DateSearchOption options);
    }

    public class GrnDbViewService : IGrnDbViewService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public GrnDbViewService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<IEnumerable<GrnDbView>> GetAll()
        {
            return await _mijmsApiService.GetEnumerable<GrnDbView>($"api/grnDbView/");
        }

        public async Task<IEnumerable<GrnDbView>> GetAll(DateSearchOption options)
        {

            return await _mijmsApiService.PostEnumerable<GrnDbView, DateSearchOption>($"api/grnDbView/filter", options);


        }
        /*        public async Task<ProductView> GetProduct(string productId)
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
                } */
    }
}
