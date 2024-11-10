using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IVoucherTypeService
    {
        Task<VoucherType> GetVoucherType(int voucherTypeKey);

        Task<VoucherType> CreateVoucherType(VoucherType voucherType);

    //    Task<IEnumerable<VoucherType>> GetAll(VoucherTypeSearchOption options);

        Task UpdateVoucherType(VoucherType voucherType);
    }

    public class VoucherTypeService : IVoucherTypeService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public VoucherTypeService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<VoucherType> CreateVoucherType(VoucherType voucherType)
        {
            return await _mijmsApiService.Post($"api/VoucherType/", voucherType);
        }

        public async Task<VoucherType> GetVoucherType(int voucherTypeKey)
        {
            return await _mijmsApiService.Get<VoucherType>($"api/VoucherType/{voucherTypeKey}");
        }

        //public async Task<IEnumerable<VoucherType>> GetAll(VoucherSearchOption options)
        //{
        //    return await _mijmsApiService.PostEnumerable<VoucherType, VoucherSearchOption>($"api/voucher/filter", options);
        //}

        public async Task UpdateVoucherType(VoucherType voucherType)
        {
            await _mijmsApiService.Put($"api/VoucherType/", voucherType);
        }
    }

}
