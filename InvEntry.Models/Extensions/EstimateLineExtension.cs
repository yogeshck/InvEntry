using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Extensions
{
    public static class EstimateLineExtension
    {
        public static void SetProductDetails(this EstimateLine line, ProductView prod)  
        {

            line.ProductGkey = prod.GKey;
            line.ProductDesc = prod.Description;
            line.ProductName = prod.Name;
            line.ProductPurity = prod.Purity;
            //line.VaPercent = prod.VA
            line.ProductId = prod.Id;
            line.Metal = prod.Metal;
            line.IsTaxable = true; 
            line.ProdCategory = prod.Category;
            line.HsnCode = prod.HsnCode;

            line.ProdGrossWeight = 0;                           // prod.GrossWeight;
            line.ProdStoneWeight = 0;                           // prod.StoneWeight;
            //line.ProductSku = prod.ProductSku;

            line.ProductPurity = prod.Purity;
            line.VaPercent = prod.VaPercent;
            line.VaAmount = 0;
            line.ProductId = prod.Id;
            line.Metal = prod.Metal;
            //line.IsTaxable = product.IsTaxable;
            line.ProdCategory = prod.Category;
            line.HsnCode = prod.HsnCode;

        }
    }
}
