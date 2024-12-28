using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class RepDailyStockSummary : BaseEntity
    {

        [ObservableProperty]
        public DateTime? _transactionDate;

        [ObservableProperty]
        public string? _productCategory;

        [ObservableProperty]
        public decimal? _openingStock;

        [ObservableProperty]
        public decimal? _stockIn;

        [ObservableProperty]
        public decimal? _stockTransferIn;

        [ObservableProperty]
        public decimal? _stockOut;

        [ObservableProperty]
        public decimal? _stockTransferOut;

        [ObservableProperty]
        public decimal? _closingStock;

        [ObservableProperty]
        public string? _metal;

        [ObservableProperty]
        public int? _openingStockQty;

        [ObservableProperty]
        public int? _stockInQty;

        [ObservableProperty]
        public int? _stockTransferInQty;

        [ObservableProperty]
        public int? _stockOutQty;

        [ObservableProperty]
        public int? _stockTrnsferOutQty;

        [ObservableProperty]
        public int? _closingStockQty;
    }
}
