using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class DailyStockSummary : BaseEntity
    {

        [ObservableProperty]
        public DateTime? _transactionDate;

        [ObservableProperty]
        public string? _metal;

        [ObservableProperty]
        public string? _productCategory;

        [ObservableProperty]
        public decimal? _openingStockGrossWeight;

        [ObservableProperty]
        public decimal? _openingStockStoneWeight;

        [ObservableProperty]
        public decimal? _openingStockNetWeight;

        [ObservableProperty]
        public decimal? _stockInGrossWeight;

        [ObservableProperty]
        public decimal? _stockInStoneWeight;

        [ObservableProperty]
        public decimal? _stockInNetWeight;

        [ObservableProperty]
        public decimal? _stockOutGrossWeight;

        [ObservableProperty]
        public decimal? _stockOutStoneWeight;

        [ObservableProperty]
        public decimal? _stockOutNetWeight;

        [ObservableProperty]
        public decimal? _closingStockGrossWeight;

        [ObservableProperty]
        public decimal? _closingStockStoneWeight;

        [ObservableProperty]
        public decimal? _closingStockNetWeight;

        [ObservableProperty]
        public int? _openingStockQty;

        [ObservableProperty]
        public int? _stockInQty;

        [ObservableProperty]
        public int? _stockOutQty;

        [ObservableProperty]
        public int? _closingStockQty;

    }
}
