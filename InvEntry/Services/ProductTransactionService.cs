using DevExpress.XtraLayout.Customization;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IProductTransactionService
    {

        Task<ProductTransaction> GetProductTransactionBySku(string productSku);

        Task<ProductTransaction> GetLastProductTransactionBySku(string lastTransaction);

        Task<ProductTransaction> GetLastProductTransactionByCategory(string lastTransaction);

        Task<ProductTransaction> GetByCategory(string category);

        Task CreateProductTransaction(ProductTransaction productTrans);

        Task UpdateProductTransaction(ProductTransaction productTrans);
    }

    public class ProductTransactionService : IProductTransactionService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public ProductTransactionService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<ProductTransaction> GetProductTransactionBySku(string productSku)
        {
            return await _mijmsApiService.Get<ProductTransaction>($"api/productTransaction/{productSku}");
        }

        public async Task<ProductTransaction> GetLastProductTransactionBySku(string lastTransaction)
        {
            return await _mijmsApiService.Get<ProductTransaction>($"api/productTransaction/{lastTransaction}");
        }

        public async Task<ProductTransaction> GetByCategory(string category)
        {
            return await _mijmsApiService.Get<ProductTransaction>($"api/productTransaction/{category}");
        }

        public async Task CreateProductTransaction(ProductTransaction productTrans)
        {
            await _mijmsApiService.Post($"api/productTransaction/", productTrans);
        }

        public async Task UpdateProductTransaction(ProductTransaction productTrans)
        {
            await _mijmsApiService.Put($"api/productTransaction/{productTrans.GKey}", productTrans);
        }

        public async Task<ProductTransaction> GetLastProductTransactionByCategory(string category)
        {
            return await _mijmsApiService.Get<ProductTransaction>($"api/productTransaction/lastTransaction/Category/{category}");
        }
    }
}
