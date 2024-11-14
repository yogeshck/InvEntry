using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IGrnService
    {
        Task<GrnHeader> GetHeader(string grnNbr);

        Task<GrnHeader> CreateHeader(GrnHeader grnHdr);

        Task UpdateHeader(GrnHeader grnHdr);

        Task<IEnumerable<GrnHeader>> GetAll(DateSearchOption options);

        Task CreateGrnLine(GrnLine line);

        Task CreateGrnLine(IEnumerable<GrnLine> line);
    }

    public class GrnService : IGrnService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public GrnService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<GrnHeader> GetHeader(string grnNbr)
        {
            return await _mijmsApiService.Get<GrnHeader>($"api/grn/{grnNbr}");
        }

        public async Task<GrnHeader> CreateHeader(GrnHeader grnHdr)
        {
            return await _mijmsApiService.Post($"api/grn/", grnHdr);
        }

        public async Task UpdateHeader(GrnHeader grnHdr)
        {
            await _mijmsApiService.Put($"api/grn/{grnHdr.GrnNbr}", grnHdr);
        }

        public async Task CreateGrnLine(GrnLine line)
        {
            await _mijmsApiService.Post($"api/grnline/", line);
        }

        public async Task CreateGrnLine(IEnumerable<GrnLine> lines)
        {
            var list = new List<Task>();

            foreach (var line in lines)
                list.Add(CreateGrnLine(line));

            await Task.WhenAll(list);
        }

        public async Task<IEnumerable<GrnHeader>> GetAll(DateSearchOption options)
        {

            return await _mijmsApiService.PostEnumerable<GrnHeader, DateSearchOption>($"api/grn/filter", options);


        }
    }
}
