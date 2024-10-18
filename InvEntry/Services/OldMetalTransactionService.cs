using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{

    public interface IOldMetalTransactionService
    {
        Task<OldMetalTransaction> GetOldMetalTransaction(string voucherId);

        Task<OldMetalTransaction> CreatOldMetalTransaction(OldMetalTransaction oldMetalTransaction);

        Task UpdateOldMetalTransaction(OldMetalTransaction oldMetalTransaction);
    }

    public class OldMetalTransactionService : IOldMetalTransactionService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public OldMetalTransactionService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<OldMetalTransaction> CreatOldMetalTransaction(OldMetalTransaction oldMetalTransaction)
        {
            return await _mijmsApiService.Post($"api/OldMetalTransaction/", oldMetalTransaction);
        }


        public async Task<OldMetalTransaction> GetOldMetalTransaction(string invoiceNbr)
        {
            return await _mijmsApiService.Get<OldMetalTransaction>($"api/OldMetalTransaction/{invoiceNbr}");
        }

        public async Task UpdateOldMetalTransaction(OldMetalTransaction oldMetalTransaction)
        {
            await _mijmsApiService.Put($"api/OldMetalTransaction/", oldMetalTransaction);
        }

    }
}
