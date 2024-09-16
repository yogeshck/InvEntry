using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class Product : BaseEntity
    {
        [ObservableProperty]
        private byte[]? _tenantGkey;

        [ObservableProperty]
        private string? _baseUnit;

        [ObservableProperty]
        private string? _brand;

        [ObservableProperty]
        private long? _productGkey;

        [ObservableProperty]
        private string? _hsnCode;

        [ObservableProperty]
        private string? _metal;

        [ObservableProperty]
        private string? _model;

        [ObservableProperty]
        private string? _uom;

        [ObservableProperty]
        private decimal? _grossWeight;

        [ObservableProperty]
        private decimal? _netWeight;

        [ObservableProperty]
        private decimal? _otherWeight;

        [ObservableProperty]
        private string? _productDesc;

        [ObservableProperty]
        private string? _productImageRef;

        [ObservableProperty]
        private int? _qty;

        [ObservableProperty]
        private string? _setIdGkey;

        [ObservableProperty]
        private string? _status;

        [ObservableProperty]
        private long? _stockId;

        [ObservableProperty]
        private long? _supplierId;

        [ObservableProperty]
        private decimal? _taxRule;

        [ObservableProperty]
        private bool? _taxable;

        [ObservableProperty]
        private bool? _activeForSale;

        [ObservableProperty]
        private bool? _deletedFlag;

        [ObservableProperty]
        private string? _purchaseRef;

        [ObservableProperty]
        private string? _productId;

        [ObservableProperty]
        private decimal? _vaPercent;

        [ObservableProperty]
        private string? productName;

        [ObservableProperty]
        private string? productPurity;

        [ObservableProperty]
        private string? productCategory;

        [ObservableProperty]
        private decimal? _SupplierRate;
        [ObservableProperty]
        private decimal? _MakingCharges;
        [ObservableProperty]
        private string? _CustomerOrderRefId;
        [ObservableProperty]
        private int? _Size;
        [ObservableProperty]
        private string? _SizeId;
        [ObservableProperty]
        private string? _SizeUom;
        [ObservableProperty]
        private decimal? _WastageAmount;
        [ObservableProperty]
        private decimal? _WastagePercent;
        [ObservableProperty]
        private string? _ProductSku;
        [ObservableProperty]
        private decimal? _OriginalGrossWieght;
        [ObservableProperty]
        private decimal? _AdjustedWieght;
        [ObservableProperty]
        private string? _DocRef;
        [ObservableProperty]
        private DateTime? _DocDate;
        [ObservableProperty]
        private bool? _ProductSold;
    }
}
