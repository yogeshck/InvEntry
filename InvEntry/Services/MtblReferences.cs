using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{

    public interface IMtblReferencesService
    {
        Task<MtblReference> GetReference(string refName, string refCode);

        Task<MtblReference> CreatReference(MtblReference mtblReference);

        Task UpdateReference(MtblReference mtblReference);
    }

    public class MtblReferencesService : IMtblReferencesService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public MtblReferencesService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<MtblReference> CreatReference(MtblReference mtblReference)
        {
            return await _mijmsApiService.Post($"api/MtblReference/", mtblReference);
        }

        public async Task<MtblReference> GetReference(string refName, string refCode)
        {
            return await _mijmsApiService.Get<MtblReference>($"api/MtblReference/{refName},{refCode}");
        }

        public async Task UpdateReference(MtblReference mtblReference)
        {
            await _mijmsApiService.Put($"api/MtblReference/", mtblReference);
        }


    }
}
