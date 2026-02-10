using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class GrnLine : BaseEntity
    {
        [ObservableProperty]
        public int? _grnHdrGkey;

        [ObservableProperty]
        public int? _lineNbr;

        [ObservableProperty]
        public string? _productId;

        [ObservableProperty]
        public string? _productDesc;

        [ObservableProperty]
        public int? _productGkey;

        [ObservableProperty]
        public string? _productPurity;

        [ObservableProperty]
        public string? _suppProductNbr;

        [ObservableProperty]
        public string? _suppProductDesc;

        [ObservableProperty]
        public decimal? _grossWeight;

        [ObservableProperty]
        public decimal? _stoneWeight;

        [ObservableProperty]
        public decimal? _netWeight;

        [ObservableProperty]
        public string? _uom;

        [ObservableProperty]
        public int? _orderedQty;

        [ObservableProperty]
        public int? _suppliedQty;

        [ObservableProperty]
        public int? _receivedQty;

        [ObservableProperty]
        public int? _acceptedQty;

        [ObservableProperty]
        public int? _rejectedQty;

        [ObservableProperty]
        public int? _returnedQty;

        [ObservableProperty]
        public string? _status;

        [ObservableProperty]
        public string? _notes;

        [ObservableProperty]
        public decimal? _suppVaPercent;

        [ObservableProperty]
        public decimal? _suppWastagePercent;

        [ObservableProperty]
        public decimal? _suppWastageAmount;

        [ObservableProperty]
        public decimal? _suppRate;

        [ObservableProperty]
        public decimal? _suppMakingCharges;

        [ObservableProperty]
        public int? _sizeId;

        [ObservableProperty]
        public string? _size;

        [ObservableProperty]
        public string? _sizeUom;

        [ObservableProperty]
        public int? _grnLineSumryGkey;

        [ObservableProperty]
        public string? _productSku;

        [ObservableProperty]
        private bool _isPrinted = false;


    }
}
