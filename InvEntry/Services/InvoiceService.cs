﻿using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services
{
    public interface IInvoiceService
    {
        Task<InvoiceHeader> GetHeader(string productId);

        Task<InvoiceHeader> CreatHeader(InvoiceHeader product);

        Task UpdateHeader(InvoiceHeader product);

        Task<IEnumerable<InvoiceHeader>>  GetAll(DateTime from, DateTime to);

        Task CreatInvoiceLine(InvoiceLine line);

        Task CreatInvoiceLine(IEnumerable<InvoiceLine> line);
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

        public async Task<InvoiceHeader> CreatHeader(InvoiceHeader product)
        {
            return await _mijmsApiService.Post($"api/invoice/", product);
        }

        public async Task UpdateHeader(InvoiceHeader product)
        {
            await _mijmsApiService.Put($"api/invoice/{product.InvNbr}", product);
        }

        public async Task CreatInvoiceLine(InvoiceLine line)
        {
            await _mijmsApiService.Post($"api/invoiceline/", line);
        }

        public async Task CreatInvoiceLine(IEnumerable<InvoiceLine> lines)
        {
            var list = new List<Task>();

            foreach(var line in lines)
                list.Add(CreatInvoiceLine(line));

            await Task.WhenAll(list);
        }

        public async Task<IEnumerable<InvoiceHeader>> GetAll(DateTime from, DateTime to)
        {

            return await _mijmsApiService.GetEnumerable<InvoiceHeader>($"api/invoice/{from.Date}/{to.Date}");


        }
    }
}
