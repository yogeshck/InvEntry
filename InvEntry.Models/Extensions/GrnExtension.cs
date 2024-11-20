using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Extensions
{
    public static class GrnExtension
    {
        public static void SetProductDetails(this GrnLine line, ProductView product)
        {
            line.GrossWeight = product.GrossWeight;
            line.StoneWeight = product.StoneWeight;
            line.ProductDesc = product.Description;
            line.ProductPurity = product.Purity;
            line.SuppVaPercent = product.VaPercent;
            line.ProductId = product.Id;
        }

        //public static void SetLineSummary(this GrnLineSummary line, ProductView product)
        //{
        //    line.GrossWeight = product.GrossWeight;
        //    line.StoneWeight = product.StoneWeight;
        //    line.ProductDesc = product.Description;
        //    line.ProductPurity = product.Purity;
        //    line.SuppVaPercent = product.VaPercent;
        //    line.ProductId = product.Id;
        //}
    }
}
