using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.Services
{

    public interface IOldMetalTransactionService
    {
        Task<OldMetalTransaction> GetOldMetalTransaction(string voucherId);

        Task<OldMetalTransaction> CreateOldMetalTransaction(OldMetalTransaction oldMetalTransaction);

        Task UpdateOldMetalTransaction(OldMetalTransaction oldMetalTransaction);

        Task<IEnumerable<OldMetalTransaction>> GetByDocRefNbr(string docRefNbr);

        Task <string> CreateOldMetalTransaction(IEnumerable<OldMetalTransaction> lines);

        Task<IEnumerable<OldMetalTransaction>> GetAll(DateSearchOption options);
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


        public async Task<string> CreateOldMetalTransaction(IEnumerable<OldMetalTransaction> lines)
        {
            var tasks = new List<Task<OldMetalTransaction>>();

            foreach (var line in lines)
            {
                tasks.Add(CreateOldMetalTransaction(line));
            }

            var results = await Task.WhenAll(tasks);

            return results.LastOrDefault()?.TransNbr;

        }

        public async Task<OldMetalTransaction> GetOldMetalTransaction(string transNbr)
        {
            return await _mijmsApiService.Get<OldMetalTransaction>($"api/OldMetalTransaction/{transNbr}");
        }

        public async Task<IEnumerable<OldMetalTransaction>> GetByDocRefNbr(string docRefNbr)
        {
            if(string.IsNullOrWhiteSpace(docRefNbr))
                return Enumerable.Empty<OldMetalTransaction>();

            return await _mijmsApiService
                .GetEnumerable<OldMetalTransaction>(
                    $"api/OldMetalTransaction/docRefNbr/{Uri.EscapeDataString(docRefNbr)}");
        }
        
        public async Task UpdateOldMetalTransaction(OldMetalTransaction oldMetalTransaction)
        {
            await _mijmsApiService.Put($"api/OldMetalTransaction/", oldMetalTransaction);
        }

        public async Task<IEnumerable<OldMetalTransaction>> GetAll(DateSearchOption options)
        {
            return await _mijmsApiService.PostEnumerable<OldMetalTransaction, DateSearchOption>($"api/OldMetalTransaction/filter", options);
        }

    }

}
