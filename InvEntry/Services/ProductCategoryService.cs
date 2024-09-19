using DevExpress.XtraEditors;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IProductCategoryService
    {
        Task<ProductCategory> GetProductCategory(string categoryName);

        Task<IEnumerable<ProductCategory>> GetProductCategoryList();

        Task CreatProductCategory(ProductCategory productCategory);

        Task UpdateProductCategory(ProductCategory productcategory);
    }

    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public ProductCategoryService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<ProductCategory> GetProductCategory(string categoryName)
        {
            return await _mijmsApiService.Get<ProductCategory>($"api/productcategory/{categoryName}");
        }

        public async Task CreatProductCategory(ProductCategory productCategory)
        {
            await _mijmsApiService.Post($"api/productcategory/", productCategory);
        }

        public async Task UpdateProductCategory(ProductCategory categoryName)
        {
            await _mijmsApiService.Put($"api/productcategory/{categoryName.Name}", categoryName);
        }

        public async Task<IEnumerable<ProductCategory>> GetProductCategoryList()
        {
            return await _mijmsApiService.GetEnumerable<ProductCategory>("api/productcategory/");
       
        }
    }
}
