using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models.Extensions
{
    public static class ProductStockSumryExtension
    {

        public static void EnrichStockFromEstimate(this ProductStockSummary productSumryStk, EstimateLine line)
        {

            productSumryStk.GrossWeight = (productSumryStk.GrossWeight ?? 0) - line.ProdGrossWeight;
            productSumryStk.StoneWeight = (productSumryStk.StoneWeight ?? 0) - line.ProdStoneWeight;
            productSumryStk.NetWeight = (productSumryStk.NetWeight ?? 0) - line.ProdNetWeight;
            productSumryStk.StockQty = (productSumryStk.StockQty ?? 0) - line.ProdQty;

            productSumryStk.SuppliedGrossWeight = 0;
            productSumryStk.BalanceWeight = 0; 
            productSumryStk.SoldWeight = 0; 
            productSumryStk.SoldQty = 0;

        }

        public static void EnrichStockFromInvoice(this ProductStockSummary productSumryStk, InvoiceLine line)
        {
            productSumryStk.GrossWeight = (productSumryStk.GrossWeight ?? 0) - line.ProdGrossWeight;
            productSumryStk.StoneWeight = (productSumryStk.StoneWeight ?? 0) - line.ProdStoneWeight;
            productSumryStk.NetWeight = (productSumryStk.NetWeight ?? 0) - line.ProdNetWeight;
            productSumryStk.StockQty = (productSumryStk.StockQty ?? 0) - line.ProdQty;

            productSumryStk.SuppliedGrossWeight = 0;
            productSumryStk.BalanceWeight = 0; 
            productSumryStk.SoldWeight = 0; 
            productSumryStk.SoldQty = 0; 

        }
    }
}

