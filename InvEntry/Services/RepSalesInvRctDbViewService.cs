using DevExpress.XtraLayout.Customization;
using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IRepSalesInvrctDbViewService
    {
        Task<IEnumerable<RepSalesInvrctDbView>> GetAll();
        Task<IEnumerable<RepSalesInvrctDbView>> GetAll(DateSearchOption options);
    }

    public class RepSalesInvrctDbViewService : IRepSalesInvrctDbViewService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public RepSalesInvrctDbViewService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<IEnumerable<RepSalesInvrctDbView>> GetAll()
        {
            return await _mijmsApiService.GetEnumerable<RepSalesInvrctDbView>($"api/repSalesInvrctDbView/");
        }

        public async Task<IEnumerable<RepSalesInvrctDbView>> GetAll(DateSearchOption options)
        {
            return await _mijmsApiService.PostEnumerable<RepSalesInvrctDbView, DateSearchOption>($"api/repSalesInvrctDbView/filter", options);
        }
    }
}

