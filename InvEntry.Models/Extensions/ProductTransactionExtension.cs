using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Extensions
{
    public static class ProductTransactionExtension
    {
        public static void SetProductTransDetails(this ProductTransaction productTransaction, ProductView product)
        {

            //var productTrans = await _productTransactionService.GetLastProductTransactionBySku(productStock.ProductSku);
            //if (productTrans != null)
            //{
            //    productTransaction.OpeningGrossWeight = productTrans.ClosingGrossWeight;
            //    productTransaction.OpeningStoneWeight = productTrans.ClosingStoneWeight;
            //    productTransaction.OpeningNetWeight = productTrans.ClosingNetWeight;

            //}
            //else
            //{
            //    productTransaction.OpeningGrossWeight = 0;
            //    productTransaction.OpeningStoneWeight = 0;
            //    productTransaction.OpeningNetWeight = 0;
            //}

            //productTransaction.ProductSku = productStock.ProductSku;
            //productTransaction.RefGkey = productStock.GKey;
            //productTransaction.TransactionDate = DateTime.Now;
            //productTransaction.ProductCategory = productStock.Category;

            //productTransaction.TransactionType = "Receipt";
            //productTransaction.DocumentNbr = SelectedGrn.GrnNbr;
            //productTransaction.DocumentDate = SelectedGrn.GrnDate;
            //productTransaction.DocumentType = "GRN";
            //productTransaction.VoucherType = "Stock Receipt";

            //productTransaction.ObQty = 0;
            //productTransaction.TransactionQty = productStock.SuppliedQty;
            //productTransaction.CbQty = productStock.SuppliedQty;

            //productTransaction.TransactionGrossWeight = productStock.GrossWeight;
            //productTransaction.TransactionStoneWeight = productStock.StoneWeight;
            //productTransaction.TransactionNetWeight = productStock.NetWeight;

            //productTransaction.ClosingGrossWeight = productTransaction.OpeningGrossWeight + productStock.GrossWeight;
            //productTransaction.ClosingStoneWeight = productTransaction.OpeningStoneWeight + productStock.StoneWeight;
            //productTransaction.ClosingNetWeight = productTransaction.OpeningNetWeight + productStock.NetWeight;

        }
    }
}
