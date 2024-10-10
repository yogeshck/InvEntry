using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{

    public interface IArInvoiceReceiptService    
    {
        Task<ArInvoiceReceipt> GetARInvReceipt(string voucherId);

        Task<ArInvoiceReceipt> CreatARInvReceipt(ArInvoiceReceipt arInvoiceReceipt);

        Task UpdateARInvReceipt(ArInvoiceReceipt arInvoiceReceipt);
    }

    public class ArInvoiceReceiptService : IArInvoiceReceiptService
    {

        private readonly IMijmsApiService _mijmsApiService;

        public ArInvoiceReceiptService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<ArInvoiceReceipt> CreatARInvReceipt(ArInvoiceReceipt arInvoiceReceipt)
        {
             return await _mijmsApiService.Post($"api/ARInvoiceReceipt/", arInvoiceReceipt);
        }


        public async Task<ArInvoiceReceipt> GetARInvReceipt(string invoiceNbr)
        {
            return await _mijmsApiService.Get<ArInvoiceReceipt>($"api/ARInvoiceReceipt/{invoiceNbr}");
        }

        public async Task UpdateARInvReceipt(ArInvoiceReceipt arInvoiceReceipt)
        {
            await _mijmsApiService.Put($"api/ARInvoiceReceipt/", arInvoiceReceipt);
        }

    }
}
