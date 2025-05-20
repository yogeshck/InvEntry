using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Extensions
{
    public static class CustOrderLineExtension
    {
        public static void SetProductDetails(this CustomerOrderLine line, ProductView product)
        {
            //will be introduced when we use barcode
            line.ProdGrossWeight = 0;                           // product.GrossWeight;
            line.ProdStoneWeight = 0;                           // product.StoneWeight;
            line.ProductId = product.Id;
            line.ProductSku = product.ProductSku;
            line.ProductGkey = product.GKey;
            line.ProdCategory = product.Category;
            line.ProductDesc = product.Description;
            line.ProductName = product.Name;
            line.ProductMetal = product.Metal;
            line.ProductPurity = product.Purity;
            line.VaPercent = product.VaPercent;
            line.VaAmount = 0;
        //    line.IsTaxable = product.IsTaxable;
         //   line.HsnCode = product.HsnCode;

        }
    }
}
