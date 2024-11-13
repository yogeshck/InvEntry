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

        Task<OldMetalTransaction> CreateOldMetalTransaction(OldMetalTransaction oldMetalTransaction);

        Task UpdateOldMetalTransaction(OldMetalTransaction oldMetalTransaction);

        Task CreateOldMetalTransaction(IEnumerable<OldMetalTransaction> lines);
    }

    public class OldMetalTransactionService : IOldMetalTransactionService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public OldMetalTransactionService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<OldMetalTransaction> CreateOldMetalTransaction(OldMetalTransaction oldMetalTransaction)
        {
            return await _mijmsApiService.Post($"api/OldMetalTransaction/", oldMetalTransaction);
        }

        public async Task CreateOldMetalTransaction(IEnumerable<OldMetalTransaction> lines)
        {
            var list = new List<Task>();

            foreach (var line in lines)
                list.Add(CreateOldMetalTransaction(line));

            await Task.WhenAll(list);
        }


        public async Task<OldMetalTransaction> GetOldMetalTransaction(string transNbr)
        {
            return await _mijmsApiService.Get<OldMetalTransaction>($"api/OldMetalTransaction/{transNbr}");
        }

        public async Task UpdateOldMetalTransaction(OldMetalTransaction oldMetalTransaction)
        {
            await _mijmsApiService.Put($"api/OldMetalTransaction/", oldMetalTransaction);
        }

    }
}
