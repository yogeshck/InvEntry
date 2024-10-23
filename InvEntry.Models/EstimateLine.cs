using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class EstimateLine : BaseEntity
    {

        [ObservableProperty]
        public string? _hsnCode;

        [ObservableProperty]
        public int? _estLineNbr;

        [ObservableProperty]
        public decimal? _estlBilledPrice;

        [ObservableProperty]
        public decimal? _estlGrossAmt;

        [ObservableProperty]
        public decimal? _estlMakingCharges;

        [ObservableProperty]
        public decimal? _estlOtherCharges;

        [ObservableProperty]
        public decimal? _estlPayableAmt;

        [ObservableProperty]
        public decimal? _estlStoneAmount;

        [ObservableProperty]
        public decimal? _estlTaxableAmount;

        [ObservableProperty]
        public decimal? _estlWastageAmt;

        [ObservableProperty]
        public string? _prodCategory;

        [ObservableProperty]
        public decimal? _prodGrossWeight;

        [ObservableProperty]
        public decimal? _prodNetWeight;

        [ObservableProperty]
        public int _prodQty;

        [ObservableProperty]
        public decimal? _prodStoneWeight;

        [ObservableProperty]
        public string? _productDesc;

        [ObservableProperty]
        public int? _productGkey;

        [ObservableProperty]
        public string? _productName;

        [ObservableProperty]
        public string? _prodPackCode;

        [ObservableProperty]
        public string? _productPurity;

        [ObservableProperty]
        public bool? _isTaxable;

        [ObservableProperty]
        public string? _itemNotes;

        [ObservableProperty]
        public bool? _itemPacked;

        [ObservableProperty]
        public decimal? _estlCgstPercent;

        [ObservableProperty]
        public decimal? _estlCgstAmount;

        [ObservableProperty]
        public decimal? _estlIgstPercent;

        [ObservableProperty]
        public decimal? _estlIgstAmount;

        [ObservableProperty]
        public decimal? _estlTotal;

        [ObservableProperty]
        public decimal? _estlSgstAmount;

        [ObservableProperty]
        public decimal? _estlSgstPercent;

        [ObservableProperty]
        public string? _productId;

        [ObservableProperty]
        public string? _metal;

        [ObservableProperty]
        public decimal? _taxAmount;

        [ObservableProperty]
        public decimal? _taxPercent;

        [ObservableProperty]
        public string? _taxType;

        [ObservableProperty]
        public decimal? _vaAmount;

        [ObservableProperty]
        public decimal? _vaPercent;

        [ObservableProperty]
        public string? _estNote;

        [ObservableProperty]
        public int? _estimateHdrGkey;

        [ObservableProperty]
        public string? _estimateId;

        [ObservableProperty]
        public string? _createdBy;

        [ObservableProperty]
        public DateTime? _createdOn;

        [ObservableProperty]
        public string? _modifiedBy;

        [ObservableProperty]
        public DateTime? _modifiedOn;

        [ObservableProperty]
        public int? _tenantGkey;

}
}
