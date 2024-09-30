using DevExpress.CodeParser;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IFinDayBookService
{
    Task<Voucher> GetVoucher(string voucherId);

    Task<Voucher> CreatVoucher(Voucher voucher);

    Task UpdateVoucher(Voucher voucher);
}

public class FinDayBookService : IFinDayBookService
{

    private readonly IMijmsApiService _mijmsApiService;

    public FinDayBookService(IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public async Task<Voucher> CreatVoucher(Voucher voucher)
    {
        return await _mijmsApiService.Post($"api/FinDayBook/", voucher);
    }

    public async Task<Voucher> GetVoucher(string voucherId)
    {
        return await _mijmsApiService.Get<Voucher>($"api/FinDayBook/{voucherId}");
    }

    public async Task UpdateVoucher(Voucher voucher)
    {
        await _mijmsApiService.Put($"api/FinDayBook/", voucher);
    }




}
