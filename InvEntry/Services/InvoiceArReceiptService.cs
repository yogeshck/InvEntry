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
        Task<InvoiceArReceipt> GetInvArReceipt(string invoiceNbr);

        Task<InvoiceArReceipt> CreateInvArReceipt(InvoiceArReceipt invoiceArReceipt);

        Task UpdateInvArReceipt(InvoiceArReceipt invoiceArReceipt);

        Task<IEnumerable<InvoiceArReceipt>> GetByInvHdrGKey(int hdrGkey);
    }

    public class InvoiceArReceiptService : IInvoiceArReceiptService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public InvoiceArReceiptService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<InvoiceArReceipt> CreateInvArReceipt(InvoiceArReceipt invoiceArReceipt)
        {
             return await _mijmsApiService.Post($"api/InvoiceArReceipt/", invoiceArReceipt);
        }


        public async Task<InvoiceArReceipt> GetInvArReceipt(string invoiceNbr)
        {
            return await _mijmsApiService.Get<InvoiceArReceipt>($"api/InvoiceArReceipt/{invoiceNbr}");
        }

        public async Task<IEnumerable<InvoiceArReceipt>> GetByInvHdrGKey(int hdrGkey)
        {
            return await _mijmsApiService.GetEnumerable<InvoiceArReceipt>($"api/InvoiceArReceipt/hdrGkey/{hdrGkey}");
        }

        public async Task UpdateInvArReceipt(InvoiceArReceipt invoiceArReceipt)
        {
            await _mijmsApiService.Put($"api/InvoiceArReceipt/", invoiceArReceipt);
        }

    }
}
