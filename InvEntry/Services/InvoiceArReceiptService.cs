using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{

    public interface IInvoiceArReceiptService    
    {
        Task<InvoiceArReceipt> GetInvArReceipt(string voucherId);

        Task<InvoiceArReceipt> CreatInvArReceipt(InvoiceArReceipt invoiceArReceipt);

        Task UpdateInvArReceipt(InvoiceArReceipt invoiceArReceipt);
    }

    public class InvoiceArReceiptService : IInvoiceArReceiptService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public InvoiceArReceiptService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<InvoiceArReceipt> CreatInvArReceipt(InvoiceArReceipt invoiceArReceipt)
        {
             return await _mijmsApiService.Post($"api/InvoiceArReceipt/", invoiceArReceipt);
        }


        public async Task<InvoiceArReceipt> GetInvArReceipt(string invoiceNbr)
        {
            return await _mijmsApiService.Get<InvoiceArReceipt>($"api/InvoiceArReceipt/{invoiceNbr}");
        }

        public async Task UpdateInvArReceipt(InvoiceArReceipt invoiceArReceipt)
        {
            await _mijmsApiService.Put($"api/InvoiceArReceipt/", invoiceArReceipt);
        }

    }
}
