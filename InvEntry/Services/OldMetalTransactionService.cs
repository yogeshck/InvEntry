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

        Task CreatOldMetalTransaction(IEnumerable<OldMetalTransaction> lines);
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

        public async Task CreatOldMetalTransaction(IEnumerable<OldMetalTransaction> lines)
        {
            var list = new List<Task>();

            foreach (var line in lines)
                list.Add(CreatOldMetalTransaction(line));

            await Task.WhenAll(list);
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
