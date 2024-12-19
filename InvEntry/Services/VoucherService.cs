using DevExpress.CodeParser;
using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IVoucherService
{
    Task<Voucher> GetVoucher(string voucherId);

    Task<Voucher> CreateVoucher(Voucher voucher);

    Task<IEnumerable<Voucher>> GetAll(VoucherSearchOption options);

    Task UpdateVoucher(Voucher voucher);
}

public class VoucherService : IVoucherService
{

    private readonly IMijmsApiService _mijmsApiService;

    public VoucherService(IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public async Task<Voucher> CreateVoucher(Voucher voucher)
    {
        return await _mijmsApiService.Post($"api/Voucher/", voucher);
    }

    public async Task<Voucher> GetVoucher(string voucherId)
    {
        return await _mijmsApiService.Get<Voucher>($"api/Voucher/{voucherId}");
    }

    public async Task<IEnumerable<Voucher>> GetAll(VoucherSearchOption options)
    {
        return await _mijmsApiService.PostEnumerable<Voucher, VoucherSearchOption>($"api/voucher/filter", options);
    }

    public async Task UpdateVoucher(Voucher voucher)
    {
        await _mijmsApiService.Put($"api/Voucher/", voucher);
    }




}
