using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class ProductView : BaseEntity
    {
        [ObservableProperty]
        public string? _id;

        [ObservableProperty]
        public string? _name;

        [ObservableProperty]
        public string? _description;

        [ObservableProperty]
        public string? _category;

        [ObservableProperty]
        public string? _purity;

        [ObservableProperty]
        public string? _metal;

        [ObservableProperty]
        public string? _hsnCode;

        [ObservableProperty]
        public string? _uom;

        [ObservableProperty]
        public bool? _isTaxable;

        [ObservableProperty]
        public string? _productSku;

        [ObservableProperty]
        public decimal? _grossWeight;

        [ObservableProperty]
        public decimal? _stoneWeight;

        [ObservableProperty]
        public decimal? _netWeight;

        [ObservableProperty]
        public decimal? _vaPercent;

        [ObservableProperty]
        public decimal? _wastagePercent;

        [ObservableProperty]
        public decimal? _wastageAmount;

        [ObservableProperty]
        public decimal? _soldWeight;

        [ObservableProperty]
        public decimal? _balanceWeight;

        [ObservableProperty]
        public int? _soldQty;

        [ObservableProperty]
        public int? _stockQty;

        [ObservableProperty]
        public string? _size;

        [ObservableProperty]
        public int? _sizeId;

        [ObservableProperty]
        public string? _sizeUom;

        [ObservableProperty]
        public bool? _isProductSold;
    }
}
