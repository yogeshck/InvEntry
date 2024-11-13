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
            line.ProdGrossWeight = product.GrossWeight;
            line.ProdStoneWeight = product.StoneWeight;
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
