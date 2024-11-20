using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IInvoiceService
    {
        Task<InvoiceHeader> GetHeader(string invNbr);

        Task<InvoiceHeader> CreateHeader(InvoiceHeader invHdr);

        Task UpdateHeader(InvoiceHeader invHdr);

        Task<IEnumerable<InvoiceHeader>>  GetAll(DateSearchOption options);

        Task CreateInvoiceLine(InvoiceLine line);

        Task CreateInvoiceLine(IEnumerable<InvoiceLine> line);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly IMijmsApiService _mijmsApiService;

        public InvoiceService(IMijmsApiService mijmsApiService)
        {
            _mijmsApiService = mijmsApiService;
        }

        public async Task<InvoiceHeader> GetHeader(string invNbr)
        {
            return await _mijmsApiService.Get<InvoiceHeader>($"api/invoice/{invNbr}");
        }

        public async Task<InvoiceHeader> CreateHeader(InvoiceHeader invHdr)
        {
            return await _mijmsApiService.Post($"api/invoice/", invHdr);
        }

        public async Task UpdateHeader(InvoiceHeader invHdr)
        {
            await _mijmsApiService.Put($"api/invoice/{invHdr.InvNbr}", invHdr);
        }

        public async Task CreateInvoiceLine(InvoiceLine line)
        {
            await _mijmsApiService.Post($"api/invoiceline/", line);
        }

        public async Task CreateInvoiceLine(IEnumerable<InvoiceLine> lines)
        {
            var list = new List<Task>();

            foreach(var line in lines)
                list.Add(CreateInvoiceLine(line));

            await Task.WhenAll(list);
        }

        public async Task<IEnumerable<InvoiceHeader>> GetAll(DateSearchOption options)
        {

            return await _mijmsApiService.PostEnumerable<InvoiceHeader, DateSearchOption>($"api/invoice/filter", options);


        }
    }
}
