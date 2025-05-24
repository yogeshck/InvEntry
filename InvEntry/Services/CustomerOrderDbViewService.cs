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
    public interface ICustomerOrderDbViewService
    {

        /*        Task<GrnDbView> GetProduct(string productId);

                Task<GrnDbView> GetByProductSku(string productSku);

                Task<IEnumerable<GrnDbView>> GetByCategory(string category);*/

        Task<IEnumerable<CustomerOrderDBView>> GetAll();

        Task<IEnumerable<CustomerOrderDBView>> GetAll(DateSearchOption options);

    }

    public class CustomerOrderDbViewService : ICustomerOrderDbViewService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public CustomerOrderDbViewService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<IEnumerable<CustomerOrderDBView>> GetAll()
        {            return await _mijmsApiService.GetEnumerable<CustomerOrderDBView>($"api/customerOrderDBView/");
        }

        public async Task<IEnumerable<CustomerOrderDBView>> GetAll(DateSearchOption options)
        {

            return await _mijmsApiService.PostEnumerable<CustomerOrderDBView, DateSearchOption>($"api/customerOrderDBView/filter", options);


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