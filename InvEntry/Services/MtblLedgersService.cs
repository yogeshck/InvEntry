using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IMtblLedgersService
    {
        Task<MtblLedger> GetLedger(int lAccountCode);

        Task<IEnumerable<MtblLedger>> GetLedgerList(string accGroupName);
    }

    public class MtblLedgersService : IMtblLedgersService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public MtblLedgersService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<MtblLedger> GetLedger(int lAccountCode)
        {
            return await _mijmsApiService.Get<MtblLedger>($"api/MtblLedgers/{lAccountCode}");
        }

        public async Task<IEnumerable<MtblLedger>> GetLedgerList(string accGroupName)
        {
            return await _mijmsApiService.GetEnumerable<MtblLedger>($"api/MtblLedgers/accountGroup/{accGroupName}");
        }


    }
}
