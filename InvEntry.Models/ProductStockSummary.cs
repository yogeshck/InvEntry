using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class ProductStockSummary : BaseEntity
    {

        [ObservableProperty]
        public int? _productGkey;

        [ObservableProperty]
        public decimal? _grossWeight;

        [ObservableProperty]
        public decimal? _stoneWeight;

        [ObservableProperty]
        public decimal? _netWeight;

        [ObservableProperty]
        public decimal? _suppliedGrossWeight;

        [ObservableProperty]
        public decimal? _adjustedWeight;

        [ObservableProperty]
        public decimal? _soldWeight;

        [ObservableProperty]
        public decimal? _balanceWeight;

        [ObservableProperty]
        public int? _suppliedQty;

        [ObservableProperty]
        public int? _soldQty;

        [ObservableProperty]
        public int? _stockQty;

        [ObservableProperty]
        public string? _status;

        [ObservableProperty]
        public decimal? _vaPercent;

        [ObservableProperty]
        public decimal? _wastagePercent;

        [ObservableProperty]
        public decimal? _wastageAmount;

        [ObservableProperty]
        public decimal? _supplierRate;

        [ObservableProperty]
        public decimal? _makingCharges;

        [ObservableProperty]
        public int? _sizeId;

        [ObservableProperty]
        public string? _size;

        [ObservableProperty]
        public string? _sizeUom;

        [ObservableProperty]
        public bool? _isProductSold;

        [ObservableProperty]
        public string? _supplierId;

        [ObservableProperty]
        public string? _purchaseOrderNbr;

        [ObservableProperty]
        public string? _customerOrderNbr;

        [ObservableProperty]
        public string? _documentRef;

        [ObservableProperty]
        public DateTime? _documentDate;

    }
}
