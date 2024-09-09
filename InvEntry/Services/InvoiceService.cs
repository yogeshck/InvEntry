using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tern.MI.InvEntry.Models;

namespace InvEntry.Services
{
    public interface IInvoiceService
    {
        void SaveInvoice(InvoiceHeader invoiceHeader);
    }

    public class InvoiceService : IInvoiceService
    {


        public void SaveInvoice(InvoiceHeader invoiceHeader)
        {
            
        }
    }
}
