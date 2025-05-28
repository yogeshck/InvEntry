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

        oldMetalTransaction.TransType = setTransType(oldMetalTransaction.Metal);

    }

    public static void EnrichCustOrderDetails(this OldMetalTransaction oldMetalTransaction, CustomerOrder custOrder)
    {

        oldMetalTransaction.DocRefGkey = custOrder.GKey;
        oldMetalTransaction.DocRefNbr = custOrder.InvNbr;
        oldMetalTransaction.DocRefDate = custOrder.InvDate;
        oldMetalTransaction.CustGkey = custOrder.CustGkey;
        oldMetalTransaction.CustMobile = custOrder.CustMobileNbr;

        oldMetalTransaction.TransType = setTransType(oldMetalTransaction.Metal);
    }

    private static string setTransType(string metal)
    {
        var transType = "";

        if (metal == "OLD SILVER")
            transType = "OS Purchase";
        else if (metal == "DIAMOND")
            transType = "DIA Purchase";
        else
            transType = "OG Purchase";

        return transType;
    }

}
