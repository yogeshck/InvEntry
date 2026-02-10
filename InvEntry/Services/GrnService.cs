using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace InvEntry.Services
{
    public interface IGrnService
    {
        Task<GrnHeader> GetHeader(string grnNbr);

        Task<GrnHeader> CreateHeader(GrnHeader grnHdr);

        Task UpdateHeader(GrnHeader grnHdr);

        Task<IEnumerable<GrnHeader>> GetBySupplier(string supplier);

        Task<IEnumerable<GrnHeader>> GetAll(DateSearchOption options);

        Task CreateGrnLine(GrnLine line);

        Task CreateGrnLine(IEnumerable<GrnLine> line);

        Task<IEnumerable<GrnLine>> GetByLineSumryGkey(int lineSumryGkey);

        Task<IEnumerable<GrnLine>> GetByLineSumryGkey(int lineSumryGkey, int hdrGkey);

        Task CreateGrnLineSummary(GrnLineSummary lineSumry);

        Task CreateGrnLineSummary(IEnumerable<GrnLineSummary> lineSumry);

        Task<IEnumerable<GrnLine>> GetByHdrGkey(int hdrGkey);

        Task<IEnumerable<GrnLineSummary>> GetBySumryHdrGkey(int hdrGkey);
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

        public async Task<IEnumerable<GrnHeader>> GetBySupplier(string supplier)
        {
            return await _mijmsApiService.GetEnumerable<GrnHeader>($"api/grn/supplierId/{supplier}");
        }

        public async Task<IEnumerable<GrnLine>> GetByLineSumryGkey(int lineSumryGkey)
        {
            return await _mijmsApiService.GetEnumerable<GrnLine>($"api/grnline/lines/{lineSumryGkey}");
        }

        public async Task<IEnumerable<GrnLine>> GetByLineSumryGkey(int lineSumryGkey, int hdrGkey)
        {
            return await _mijmsApiService.GetEnumerable<GrnLine>($"api/grnline/linesummary/{lineSumryGkey}/header/{hdrGkey}");

            // linesummary /{ lineSummaryId}/ header /{ hdrId}            ")]
        }

        public async Task<IEnumerable<GrnLine>> GetByHdrGkey(int hdrGkey)
        {
            return await _mijmsApiService.GetEnumerable<GrnLine>($"api/grnline/hdrGkey/{hdrGkey}");
        }

        public async Task CreateGrnLineSummary(GrnLineSummary lineSumry)
        {
            await _mijmsApiService.Post($"api/GrnLineSummary/", lineSumry);
        }

        public async Task CreateGrnLineSummary(IEnumerable<GrnLineSummary> lineSumry)
        {
            var list = new List<Task>();

            foreach (var line in lineSumry)
                list.Add(CreateGrnLineSummary(line));

            await Task.WhenAll(list);
        }

        public async Task<IEnumerable<GrnLineSummary>> GetBySumryHdrGkey(int hdrGkey)
        {
            return await _mijmsApiService.GetEnumerable<GrnLineSummary>($"api/GrnLineSummary/{hdrGkey}");
        }


        // GET api/grn/lines?hdrId=5
/*        [HttpGet("lines")]
        public async Task<IActionResult> GetLines([FromQuery] int? hdrId, [FromQuery] int? lineSummaryId)
        {
            if (hdrId.HasValue)
                return Ok(_grnLineRepository.GetList(x => x.GrnHdrGkey == hdrId.Value));

            if (lineSummaryId.HasValue)
                return Ok(_grnLineRepository.GetList(x => x.LineSummaryGkey == lineSummaryId.Value));

            return BadRequest("Please provide either hdrId or lineSummaryId.");
        }*/


    }
}
