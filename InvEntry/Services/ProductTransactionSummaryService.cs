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
    public interface IProductTransactionSummaryService
    {
        Task<IEnumerable<ProductTransactionSummary>> GetAll(DateSearchOption options);

        Task<ProductTransactionSummary> GetLastProductTranSumryByCategory(string category);

        Task<ProductTransactionSummary> CreateProductTransactionSummary(ProductTransactionSummary productTransSumry);

        Task UpdateProductTransactionSummary(ProductTransactionSummary productTransSumry);
    }

    public class ProductTransactionSummaryService : IProductTransactionSummaryService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public ProductTransactionSummaryService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<ProductTransactionSummary> CreateProductTransactionSummary(ProductTransactionSummary productTransSumry)
        {
            return await _mijmsApiService.Post($"api/ProductTransactionSummary/", productTransSumry);
        }


        public async Task<IEnumerable<ProductTransactionSummary>> GetAll(DateSearchOption options)
        {
            return await _mijmsApiService
                        .PostEnumerable<ProductTransactionSummary, DateSearchOption>
                                                    ($"api/ProductTransactionSummary/filter", options);
        }

        public async Task<ProductTransactionSummary> GetLastProductTranSumryByCategory(string category)
        {
            return await _mijmsApiService.Get<ProductTransactionSummary>($"api/ProductTransactionSummary/lastTransaction/Category/{category}");
        }

        public async Task UpdateProductTransactionSummary(ProductTransactionSummary productTransSumry)
        {
            await _mijmsApiService.Put($"api/ProductTransactionSummary/{productTransSumry.GKey}", productTransSumry);
        }

    }

}
