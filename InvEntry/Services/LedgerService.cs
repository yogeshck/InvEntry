using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface ILedgerService
    {
        Task<LedgersHeader> GetHeader(int? ledgerGkey, int? custGkey);

        Task<LedgersHeader> CreateHeader(LedgersHeader ledgersHeader);
        
        Task UpdateHeader(LedgersHeader ledgersHeader);

        //Task<IEnumerable<LedgersHeader>> GetAll(DateSearchOption options);
            
        Task CreateLedgersTransactions(LedgersTransactions ledgersTransactions);

        Task CreateLedgersTransactions(IEnumerable<LedgersTransactions> ledgersTransactions);

    }

    public class LedgerService : ILedgerService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public LedgerService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<LedgersHeader> CreateHeader(LedgersHeader ledgersHeader)
        {
            return await _mijmsApiService.Post($"api/LedgersHeader/", ledgersHeader);
        }

        public async Task CreateLedgersTransactions(LedgersTransactions ledgersTransactions)
        {
            await _mijmsApiService.Post($"api/LedgersTransaction/", ledgersTransactions);
        }

        public async Task CreateLedgersTransactions(IEnumerable<LedgersTransactions> ledgersTransactions)
        {
            var list = new List<Task>();

            foreach (var line in ledgersTransactions)
                list.Add(CreateLedgersTransactions(line));

            await Task.WhenAll(list);
        }

        public async Task<LedgersHeader> GetHeader(int? ledgerGkey, int? custGkey)
        {
            return await _mijmsApiService.Get<LedgersHeader>($"api/LedgersHeader/{ledgerGkey}/{custGkey}");
        }

        public async Task UpdateHeader(LedgersHeader ledgersHeader)
        {
            await _mijmsApiService.Put($"api/LedgersHeader/{ledgersHeader.GKey}", ledgersHeader);
        }

    }

}
