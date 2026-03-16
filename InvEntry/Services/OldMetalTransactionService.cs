using InvEntry.Models;
using InvEntry.Utils.Options;
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

        /*        public async Task<string> CreateOldMetalTransaction(IEnumerable<OldMetalTransaction> lines)
                {
                    var list = new List<Task>();
                    var transNbr = "";

                    foreach (var line in lines) 
                    {
                        list.Add(CreateOldMetalTransaction(line));
                        transNbr = line.TransNbr;
                    }

                    await Task.WhenAll(list);

                    return transNbr;
                }*/

        public async Task<string> CreateOldMetalTransaction(IEnumerable<OldMetalTransaction> lines)
        {
            var tasks = new List<Task<OldMetalTransaction>>();

            foreach (var line in lines)
            {
                tasks.Add(CreateOldMetalTransaction(line));
            }

            var results = await Task.WhenAll(tasks);

            // If you want the last created transaction number:
            return results.Last().TransNbr;
        }

        public async Task<OldMetalTransaction> GetOldMetalTransaction(string transNbr)
        {
            return await _mijmsApiService.Get<OldMetalTransaction>($"api/OldMetalTransaction/{transNbr}");
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
