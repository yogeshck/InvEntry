using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Extension;

public static class OldMetalTransactionExtension
{
    public static void EnrichHeaderDetails(this OldMetalTransaction oldMetalTransaction,InvoiceHeader invoiceHeader)
    {
        oldMetalTransaction.DocRefGkey = invoiceHeader.GKey;
        oldMetalTransaction.DocRefNbr = invoiceHeader.InvNbr;
        oldMetalTransaction.DocRefDate = invoiceHeader.InvDate;
        oldMetalTransaction.CustGkey = invoiceHeader.CustGkey;
        oldMetalTransaction.CustMobile = invoiceHeader.CustMobile;

        if ( oldMetalTransaction.Metal == "OLD GOLD" )
            {
                oldMetalTransaction.TransType = "OG Purchase";
            } else
                    if (oldMetalTransaction.Metal == "OLD SILVER")
                    {
                       oldMetalTransaction.TransType = "OS Purchase";
                    }
    }
}
