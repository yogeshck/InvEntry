﻿using DevExpress.CodeParser;
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

        Task<IEnumerable<RepDailyStockSummary>> GetAll(DateSearchOption options);

    }

    public class DailyStockSummaryService : IDailyStockSummaryService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public DailyStockSummaryService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<IEnumerable<RepDailyStockSummary>> GetAll()
        {
            return await _mijmsApiService.GetEnumerable<RepDailyStockSummary>($"api/dailyStockSummary/");
        }

        public async Task<IEnumerable<RepDailyStockSummary>> GetAll(DateSearchOption options)
        {
            return await _mijmsApiService.PostEnumerable<RepDailyStockSummary, DateSearchOption>($"api/dailyStockSummary/filter", options);
        }
    }
}
