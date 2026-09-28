using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class ProductTransaction : BaseEntity
    {


        [ObservableProperty]
        public int? _refGkey;

        [ObservableProperty]
        public DateTime? _transactionDate;

        [ObservableProperty]
        public string? _productCategory;

        [ObservableProperty]
        public string? _productSku;

        [ObservableProperty]
        public string? _transactionType;

        [ObservableProperty]
        public string? _documentNbr;

        [ObservableProperty]
        public string? _documentType;

        [ObservableProperty]
        public string? _voucherType;

        [ObservableProperty]
        public int? _obQty;

        [ObservableProperty]
        public int? _transactionQty;

        [ObservableProperty]
        public int? _cbQty;

        [ObservableProperty]
        public decimal? _unitPrice;

        [ObservableProperty]
        public decimal? _transactionValue;

        [ObservableProperty]
        public string? _notes;

        [ObservableProperty]
        public DateTime? _documentDate;

        [ObservableProperty]
        public decimal? _openingGrossWeight;

        [ObservableProperty]
        public decimal? _openingStoneWeight;

        [ObservableProperty]
        public decimal? _openingNetWeight;

        [ObservableProperty]
        public decimal? _transactionGrossWeight;

        [ObservableProperty]
        public decimal? _transactionStoneWeight;

        [ObservableProperty]
        public decimal? _transactionNetWeight;

        [ObservableProperty]
        public decimal? _closingGrossWeight;

        [ObservableProperty]
        public decimal? _closingStoneWeight;

        [ObservableProperty]
        public decimal? _closingNetWeight;

    }
}
