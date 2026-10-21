using InvEntry.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IOrgCompanyService
    {
        Task<IEnumerable<OrgCompany>> GetAll();

        Task UpdateOrgCompany(OrgCompany company);
    }

    public class OrgCompanyService : IOrgCompanyService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public OrgCompanyService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<IEnumerable<OrgCompany>> GetAll()
        {
            return await _mijmsApiService.GetEnumerable<OrgCompany>("api/OrgCompany");
        }

        // The API route takes the numeric GKey.
        public async Task UpdateOrgCompany(OrgCompany company)
        {
            await _mijmsApiService.Put($"api/OrgCompany/{company.GKey}", company);
        }
    }
}
