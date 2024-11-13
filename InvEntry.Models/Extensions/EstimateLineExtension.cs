using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Extensions
{
    public static class EstimateLineExtension
    {
        public static void SetProductDetails(this EstimateLine line, ProductStock product)
        {
            line.ProdGrossWeight = product.GrossWeight;
            line.ProdStoneWeight = product.StoneWeight;
            line.ProductDesc = "Desc"; // product.ProductDesc;
            line.ProductName = "Name"; // product.ProductName;
            line.ProductPurity = "916"; // product.ProductPurity;
            line.VaPercent = product.VaPercent;
            line.ProductId = "Id"; // product.ProductId;
            line.Metal = "GOLD"; // product.Metal;
            line.IsTaxable = true;  // True;  //product.Taxable;
            line.ProdCategory = ""; // product.ProductCategory;
            line.HsnCode = "HSN"; // product.HsnCode;

        }
    }
}
