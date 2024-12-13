using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class ProductTransactionSummary : BaseEntity
    {
        [ObservableProperty]
        public DateTime? _transactionDate;

        [ObservableProperty]
        public string? _productSku;

        [ObservableProperty]
        public string? _productCategory;

        [ObservableProperty]
        public int? _openingQty;

        [ObservableProperty]
        public int? _stockInQty;

        [ObservableProperty]
        public int? _stockOutQty;

        [ObservableProperty]
        public int? _closingQty;

        [ObservableProperty]
        public decimal? _openingGrossWeight;

        [ObservableProperty]
        public decimal? _openingStoneWeight;

        [ObservableProperty]
        public decimal? _openingNetWeight;

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
        public decimal? _closingGrossWeight;

        [ObservableProperty]
        public decimal? _closingStoneWeight;

        [ObservableProperty]
        public decimal? _closingNetWeight;
    }

}
