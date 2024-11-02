using DevExpress.XtraRichEdit.Forms;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{

    public interface IOrgAddressService
    {
        Task<OrgAddress> GetAddress(int gkey);

        Task<OrgAddress> CreateAddress(OrgAddress orgAddress);

        Task UpdateCustomer(OrgAddress orgAddress);
    }

    public class OrgAddressService : IOrgAddressService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public OrgAddressService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<OrgAddress> CreateAddress(OrgAddress orgAddress)
        {
            orgAddress = await _mijmsApiService.Post($"api/address/", orgAddress);
            return orgAddress;
        }

        public async Task<OrgAddress> GetAddress(int gkey)
        {
              return await _mijmsApiService.Get<OrgAddress>($"api/address/{gkey}");
        }

        public async Task UpdateCustomer(OrgAddress orgAddress)
        {
            await _mijmsApiService.Put($"api/address/", orgAddress);
        }
    }
}
