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
        public string? _category;

        [ObservableProperty]
        public int? _productGkey;

        [ObservableProperty]
        public string? _productSku;

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
        public int? _adjustedQty;

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
        public string? _uom;

        [ObservableProperty]
        public string? _createdBy;

        [ObservableProperty]
        public DateTime? _createdOn;

        [ObservableProperty]
        public string? _modifiedBy;

        [ObservableProperty]
        public DateTime? _modifiedOn;

    }
}
