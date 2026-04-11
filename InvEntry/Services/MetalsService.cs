using InvEntry.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IMetalsService
    {

        Task<Metals> GetMetal(string categoryName);

        Task<IEnumerable<Metals>> GetMetalList();

        Task CreateMetal(Metals metal);

        Task UpdateMetal(Metals metal);
    }

    public class MetalsService : IMetalsService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public MetalsService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }


        public async Task<Metals> GetMetal(string metalName)
        {
            return await _mijmsApiService.Get<Metals>($"api/metals/{metalName}");
        }

        public async Task CreateMetal(Metals metal)
        {
            await _mijmsApiService.Post($"api/metals/", metal);
        }

        public async Task UpdateMetal(Metals metal)
        {
            await _mijmsApiService.Put($"api/metals/{metal.MetalName}", metal);
        }

        public async Task<IEnumerable<Metals>> GetMetalList()
        {
            return await _mijmsApiService.GetEnumerable<Metals>("api/metals/");

        }

    }

}