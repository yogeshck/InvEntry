using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Extensions;

public static class OldMetalTransactionExtension
{
    public static void EnrichInvHeaderDetails(this OldMetalTransaction oldMetalTransaction, InvoiceHeader invoiceHeader)
    {

        oldMetalTransaction.DocRefGkey = invoiceHeader.GKey;
        oldMetalTransaction.DocRefNbr = invoiceHeader.InvNbr;
        oldMetalTransaction.DocRefDate = invoiceHeader.InvDate;
        oldMetalTransaction.CustGkey = invoiceHeader.CustGkey;
        oldMetalTransaction.CustMobile = invoiceHeader.CustMobile;

        oldMetalTransaction.TransType = setTransType(oldMetalTransaction.Metal, "Billing");

    }

    public static void EnrichCustOrderDetails(this OldMetalTransaction oldMetalTransaction, CustomerOrder custOrder)
    {

        oldMetalTransaction.DocRefGkey = custOrder.GKey;
        oldMetalTransaction.DocRefNbr = custOrder.OrderNbr;
        oldMetalTransaction.DocRefDate = custOrder.OrderDate;
        oldMetalTransaction.DocRefType = "Customer Order";
        oldMetalTransaction.CustGkey = custOrder.CustGkey;
        oldMetalTransaction.CustMobile = custOrder.CustMobileNbr;

        oldMetalTransaction.TransType = setTransType(oldMetalTransaction.Metal, "Order");
    }

    private static string setTransType(string metal, string docType)
    {
        var transType = "";

        if (docType == "Billing")
        {
            if (metal == "OLD SILVER")
                transType = "OS Purchase";
            else if (metal == "DIAMOND")
                transType = "DIA Purchase";
            else
                transType = "OG Purchase";
        } else 
            if (docType == "Order")
            {
                if (metal == "OLD SILVER")
                    transType = "OS Advance";
                else if (metal == "DIAMOND")
                    transType = "DIA Advance";
                else
                    transType = "OG Advance";
            }

        return transType;
    }

}
