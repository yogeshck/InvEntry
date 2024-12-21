using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IEstimateService
    {
        Task<EstimateHeader> GetHeader(string estNbr);

        Task<EstimateHeader> CreateHeader(EstimateHeader estHdr);

        Task UpdateHeader(EstimateHeader estHdr);

        Task<IEnumerable<EstimateHeader>> GetAll(DateSearchOption options);

        Task CreateEstimateLine(EstimateLine line);

        Task CreateEstimateLine(IEnumerable<EstimateLine> lines);
    }

    public class EstimateService : IEstimateService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public EstimateService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<EstimateHeader> GetHeader(string estNbr)
        {
            return await _mijmsApiService.Get<EstimateHeader>($"api/estimate/{estNbr}");
        }

        public async Task<EstimateHeader> CreateHeader(EstimateHeader estHdr)
        {
            return await _mijmsApiService.Post($"api/estimate/", estHdr);
        }

        public async Task UpdateHeader(EstimateHeader estHdr)
        {
            await _mijmsApiService.Put($"api/estimate/{estHdr.EstNbr}", estHdr);
        }

        public async Task CreateEstimateLine(EstimateLine line)
        {
            await _mijmsApiService.Post($"api/estimateline/", line);
        }

        public async Task CreateEstimateLine(IEnumerable<EstimateLine> lines)
        {
            var list = new List<Task>();

            foreach (var line in lines)
                list.Add(CreateEstimateLine(line));

            await Task.WhenAll(list);
        }

        public async Task<IEnumerable<EstimateHeader>> GetAll(DateSearchOption options)
        {

            return await _mijmsApiService.PostEnumerable<EstimateHeader, DateSearchOption>($"api/estimate/filter", options);


        }
    }
}
