using DevExpress.Utils;
using InvEntry;
using InvEntry.Models;
using InvEntry.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvEntry.Services;

namespace InvEntry.Managers
{
    public interface ICreditManager
    {
        Task<bool> FetchCreditBalance(Customer customer);
        Task<Voucher> FetchCreditVouchers();

        // Credit balance check  - fetch through service and check any credit balance
        // update customer table with credit balance at the time of invoicing - credit availed
        // remove or update credit avaialed = NO, if customer paid the amount fully
        // 
        //Task ApplyInvoice(ProductStock stock);
        // Task SyncWithApi(ProductStock stock);
    }

    public class CreditManager : ICreditManager
    {
        public Task<bool> FetchCreditBalance(Customer customer)
        {
            throw new NotImplementedException();
        }

        public Task<Voucher> FetchCreditVouchers()
        {
            throw new NotImplementedException();
        }
    }
}
