using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IOrgThisCompanyViewService
    {
        Task<OrgThisCompanyView> GetOrgThisCompany();
    }

    public class OrgThisCompanyViewService : IOrgThisCompanyViewService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public OrgThisCompanyViewService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<OrgThisCompanyView> GetOrgThisCompany()
        {
            return await _mijmsApiService.Get<OrgThisCompanyView>($"api/orgThisCompanyView/");
        }

    }
}
