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
        private decimal? _gkey;

        [ObservableProperty]
        private byte[]? _tenantGkey;

        [ObservableProperty]
        private string? _baseUnit;

        [ObservableProperty]
        private string? _brand;

        [ObservableProperty]
        private decimal? _productGkey;

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
        private decimal? _qty;

        [ObservableProperty]
        private string? _setIdGkey;

        [ObservableProperty]
        private string? _status;

        [ObservableProperty]
        private decimal? _stockId;

        [ObservableProperty]
        private decimal? _supplierId;

        [ObservableProperty]
        private decimal? _taxRule;

        [ObservableProperty]
        private string? _taxable;

        [ObservableProperty]
        private bool? _activeForSale;

        [ObservableProperty]
        private decimal? _deletedFlag;

        [ObservableProperty]
        private decimal? _purchaseRef;

        [ObservableProperty]
        private string? _productId;

        [ObservableProperty]
        private int? _vaPercent;
    }
}
