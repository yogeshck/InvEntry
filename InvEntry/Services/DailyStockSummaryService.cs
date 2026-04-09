using DevExpress.CodeParser;
using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{

    public interface IDailyStockSummaryService
    {

        Task<IEnumerable<DailyStockSummary>> GetAll(DateSearchOption options);

        Task CreateDailyStockSummary(List<DailyStockSummary> dailyStockSummary);

        Task UpdateDailyStockSummary(DailyStockSummary dailyStockSummary);

    }

    public class DailyStockSummaryService : IDailyStockSummaryService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public DailyStockSummaryService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<IEnumerable<DailyStockSummary>> GetAll(DateSearchOption options)
        {
            return await _mijmsApiService.PostEnumerable<DailyStockSummary, DateSearchOption>($"api/dailyStockSummary/filter", options);
        }

        public async Task CreateDailyStockSummary(List<DailyStockSummary> dailyStockSummaries)
        {
            foreach (var summary in dailyStockSummaries)
            {
                await _mijmsApiService.Post($"api/dailyStockSummary/", summary);
            }
        }

/*        public async Task CreateDailyStockSummary(DailyStockSummary dailyStockSummary)
        {

            await _mijmsApiService.Post($"api/dailyStockSummary/", dailyStockSummary);
        }
*/
        public async Task UpdateDailyStockSummary(DailyStockSummary dailyStockSummary)
        {
            await _mijmsApiService.Put($"api/dailyStockSummary/{dailyStockSummary.GKey}", dailyStockSummary);
        }
    }
}
