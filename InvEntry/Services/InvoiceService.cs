using InvEntry.Contracts.Invoices;
using InvEntry.Models;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.Services
{

    public interface IInvoiceService
    {
        // =====================================================
        // NEW INVOICE LIFECYCLE
        // =====================================================

        Task<SaveInvoiceResponse> SaveDraftAsync(
            SaveInvoiceRequest request);

        Task<InvoiceEditResponse> GetForEditAsync(
            int invoiceGkey);

        Task<IEnumerable<InvoiceHeader>> GetDraftsAsync(
            DateSearchOption options);

        Task<FinaliseInvoiceResponse> FinaliseAsync(
            FinaliseInvoiceRequest request);

        Task<CancelInvoiceResponse> CancelAsync(
            int invoiceGkey);

        // =====================================================
        // EXISTING / LEGACY
        // =====================================================

        Task<InvoiceHeader> GetHeader(
            string invNbr);

        Task<InvoiceHeader> CreateHeader(
            InvoiceHeader invHdr);

        Task UpdateHeader(
            InvoiceHeader invHdr);  

        Task<IEnumerable<InvoiceHeader>> GetAll(
            DateSearchOption options);

        Task<IEnumerable<InvoiceHeader>> GetOutStanding(
            DateSearchOption options);

        Task CreateInvoiceLine(
            InvoiceLine line);

        Task CreateInvoiceLine(
            IEnumerable<InvoiceLine> line);
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

        public async Task<FinaliseInvoiceResponse> FinaliseAsync(
            FinaliseInvoiceRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.InvoiceGkey <= 0)
                throw new ArgumentException(
                    "A valid invoice GKey is required.",
                    nameof(request));

            return await _mijmsApiService
                .Post<FinaliseInvoiceRequest, FinaliseInvoiceResponse>(
                    $"api/invoice/{request.InvoiceGkey}/finalise",
                    request);
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
            //if outstanding only
            //if (options.Filter1 == "OUTSTANDING")
            //{
            //    return await GetOutStanding(options);
            //}
            if (!string.IsNullOrEmpty(options.Filter1))  //customer mobile filter
            {
                return await _mijmsApiService.PostEnumerable<InvoiceHeader, DateSearchOption>($"api/invoice/getInvList", options); 
            }
            else
            {

                return await _mijmsApiService.PostEnumerable<InvoiceHeader, DateSearchOption>($"api/invoice/filter", options);
            }
        }

/*        public async Task<IEnumerable<InvoiceHeader>> GetInvList(DateSearchOption options)
        {

            return await _mijmsApiService.PostEnumerable<InvoiceHeader, DateSearchOption>($"api/invoice/getInvList", options); ;

        }*/

        public async Task<IEnumerable<InvoiceHeader>> GetOutStanding(DateSearchOption options)
        {
            return await _mijmsApiService.PostEnumerable<InvoiceHeader, DateSearchOption>($"api/invoice/outstanding", options);

        }

        public async Task<IEnumerable<InvoiceHeader>> GetDraftsAsync(
            DateSearchOption options)
        {
            return await _mijmsApiService
                .PostEnumerable<InvoiceHeader, DateSearchOption>(
                    "api/invoice/drafts/filter",
                    options);
        }


        public async Task<InvoiceEditResponse> GetForEditAsync(
            int invoiceGkey)
        {
            return await _mijmsApiService
                .GetResponse<InvoiceEditResponse>(
                    $"api/invoice/{invoiceGkey}/edit");
        }

        public async Task<CancelInvoiceResponse> CancelAsync(
    int invoiceGkey)
        {
            if (invoiceGkey <= 0)
            {
                throw new ArgumentException(
                    "A valid invoice GKey is required.",
                    nameof(invoiceGkey));
            }

            return await _mijmsApiService
                .Post<object, CancelInvoiceResponse>(
                    $"api/invoice/{invoiceGkey}/cancel",
                    new { });
        }

        public async Task<SaveInvoiceResponse> SaveDraftAsync(
            SaveInvoiceRequest request)
        {
            return await _mijmsApiService
                .Post<SaveInvoiceRequest, SaveInvoiceResponse>(
                    "api/invoice/draft",
                    request);
        }

    }
}
