using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Extensions
{
    public static class EstimateLineExtension
    {
        public static void SetProductDetails(this EstimateLine line, Product product)
        {
            line.ProdGrossWeight = product.GrossWeight;
            line.ProdStoneWeight = product.OtherWeight;
            line.ProductDesc = product.ProductDesc;
            line.ProductName = product.ProductName;
            line.ProductPurity = product.ProductPurity;
            line.VaPercent = product.VaPercent;
            line.ProductId = product.ProductId;
            line.Metal = product.Metal;
            line.IsTaxable = product.Taxable;
            line.ProdCategory = product.ProductCategory;
            line.HsnCode = product.HsnCode;
        }
    }
}
