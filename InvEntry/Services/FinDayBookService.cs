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

    public Task<Voucher> CreatVoucher(Voucher voucher)
    {
        throw new NotImplementedException();
    }

    public Task<Voucher> GetVoucher(string voucherId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateVoucher(Voucher voucher)
    {
        throw new NotImplementedException();
    }
}
