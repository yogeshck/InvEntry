using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Extensions
{
    public static class InvLineExtension
    {
        public static void SetProductDetails(this InvoiceLine line, ProductView product)
        {
            //will be introduced when we use barcode
            line.ProdGrossWeight = 0;                           // product.GrossWeight;
            line.ProdStoneWeight = 0;                           // product.StoneWeight;
            line.ProductSku = product.ProductSku;
            line.ProductGkey = product.GKey;
            line.ProductDesc = product.Description;
            line.ProductName = product.Name;
            line.ProductPurity = product.Purity;
            line.VaPercent = product.VaPercent;
            line.ProductId = product.Id;
            line.Metal = product.Metal;
            line.IsTaxable = product.IsTaxable;
            line.ProdCategory = product.Category;
            line.HsnCode = product.HsnCode;

        }

    }
}
