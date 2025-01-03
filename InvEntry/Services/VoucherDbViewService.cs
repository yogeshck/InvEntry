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
    public interface IVoucherDbViewService
    {

        Task<IEnumerable<VoucherDbView>> GetAll();

        Task<IEnumerable<VoucherDbView>> GetAll(DateSearchOption options);

    }

    public class VoucherDbViewService : IVoucherDbViewService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public VoucherDbViewService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<IEnumerable<VoucherDbView>> GetAll()
        {
            return await _mijmsApiService.GetEnumerable<VoucherDbView>($"api/VoucherDbView/");
        }

        public async Task<IEnumerable<VoucherDbView>> GetAll(DateSearchOption options)
        {
            return await _mijmsApiService.PostEnumerable<VoucherDbView, DateSearchOption>($"api/VoucherDbView/filter", options);

        }
    }

 }
