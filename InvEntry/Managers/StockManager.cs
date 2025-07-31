using DevExpress.Utils;
using InvEntry;
using InvEntry.Models;
using InvEntry.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InvEntry.Services;

namespace InvEntry.Managers
{
    public interface IStockManager
    {
        Task<bool> ApplyMaterialReceipt(ProductStockSummary stock);
        //Task ApplyInvoice(ProductStock stock);
       // Task SyncWithApi(ProductStock stock);
    }

    public class StockManager : IStockManager
    {

        private readonly IProductStockSummaryService _stockService;
        private readonly IProductStockSummaryService _stockSummaryService;
        private readonly Dictionary<string, ProductStock> _stockMap = new();

        public StockManager(IProductStockSummaryService stockService)
        {
            _stockService = stockService;
        }

        //    public void ApplyMaterialReceipt(IEnumerable<EstimateLine> lines, string docNbr, string performedBy)
        //    {
        /*            foreach (var line in lines)
                    {
                        var code = line.ProductCode;
                        var qty = line.Quantity;

                        if (_stockMap.TryGetValue(code, out var stock))
                        {
                            stock.AddStock(qty);
                        }

                    }*/
        //   }

        public async Task<bool> ApplyMaterialReceipt(ProductStockSummary pstk)
        {

            // var stock = await _stockService.GetProductStock(productId)
            //                  ?? throw new Exception($"Stock not found for Product ID {productId}");

            if (pstk.ProductGkey is not null)
            {
                await _stockService.UpdateProductStockSummary(pstk);
                return true;
            }
            else
            {
                return false;
            }

        }

        //       public async Task<bool> DeductFromStockAsync(string productId, decimal quantity)
        //       {
        /*            return await TryAsync(async () =>
                    {
                        var stock = await _stockService.GetByProductIdAsync(productId)
                                     ?? throw new Exception($"Stock not found for Product ID {productId}");

                        if (stock.Quantity < quantity)
                            throw new Exception($"Insufficient stock for product {productId}");

                        stock.Quantity -= quantity;
                        await _stockService.UpdateStockAsync(stock);
                        return true;
                    }, $"Failed to deduct stock for product {productId}");*/
        //     return true;    
        //    }
        // }



        /////
        /*

                    public async Task ApplyMaterialReceipt(ProductStock stock)
                    {
                        if (_stockMap.TryGetValue(stock.ProductId, out var existing))
                        {
                            existing.Quantity += stock.Quantity;
                            existing.GrossWeight += stock.GrossWeight;
                        }
                        else
                        {
                            _stockMap[stock.ProductId] = stock;
                        }

                        await _stockSummaryService.SaveOrUpdate(stock);
                        await RecordAudit(stock, "MaterialReceipt");
                        await SyncWithApi(stock);
                    }

                    public async Task ApplyInvoice(ProductStock stock)
                    {
                        if (_stockMap.TryGetValue(stock.ProductId, out var existing))
                        {
                            existing.Quantity -= stock.Quantity;
                            existing.GrossWeight -= stock.GrossWeight;
                        }
                        else
                        {
                            stock.Quantity *= -1;
                            stock.GrossWeight *= -1;
                            _stockMap[stock.ProductId] = stock;
                        }

                        await _stockSummaryService.SaveOrUpdate(stock);
                        await RecordAudit(stock, "Invoice");
                        await SyncWithApi(stock);
                    }

                    private async Task RecordAudit(ProductStock stock, string action)
                    {
                        var audit = new StockAuditEntry
                        {
                            ProductId = stock.ProductId,
                            Quantity = stock.Quantity,
                            GrossWeight = stock.GrossWeight,
                            Action = action,
                            Timestamp = DateTime.Now
                        };

                        await _stockAuditService.Log(audit);
                    }

                    public async Task SyncWithApi(ProductStock stock)
                    {
                        // Example: send to API endpoint — replace this with your real call
                        await Task.Delay(10); // simulate network delay
                    }
                }
            }*/
    }

}
