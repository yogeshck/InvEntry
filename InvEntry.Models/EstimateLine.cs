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
    private string? _hsnCode;

    [ObservableProperty]
    private int? _estLineNbr;

    [ObservableProperty]
    private string? _estNote;

    [ObservableProperty]
    private decimal? _estlBilledPrice;

    [ObservableProperty]
    private decimal? _estlGrossAmt;

    [ObservableProperty]
    private decimal? _estlMakingCharges;

    [ObservableProperty]
    private decimal? _estlOtherCharges;

    [ObservableProperty]
    private decimal? _estlPayableAmt;

    [ObservableProperty]
    private decimal? _estlStoneAmount;

    [ObservableProperty]
    private decimal? _estlTaxableAmount;

    [ObservableProperty]
    private decimal? _estlWastageAmt;

    [ObservableProperty]
    private bool? _isTaxable;

    [ObservableProperty]
    private string? _itemNotes;

    [ObservableProperty]
    private int? _itemPacked;

    [ObservableProperty]
    private string? _prodCategory;

    [ObservableProperty]
    private decimal? _prodGrossWeight;

    [ObservableProperty]
    private decimal? _prodNetWeight;

    [ObservableProperty]
    private int? _prodQty;

    [ObservableProperty]
    private decimal? _prodStoneWeight;

    [ObservableProperty]
    private string? _productDesc;

    [ObservableProperty]
    private long? _productGkey;

    [ObservableProperty]
    private string? _productName;

    [ObservableProperty]
    private string? _prodPackCode;

    [ObservableProperty]
    private string? _productPurity;

    [ObservableProperty]
    private decimal? _taxAmount;

    [ObservableProperty]
    private decimal? _taxPercent;

    [ObservableProperty]
    private string? _taxType;

    [ObservableProperty]
    private decimal? _vaAmount;

    [ObservableProperty]
    private decimal? _vaPercent;

    [ObservableProperty]
    private decimal? _estHdrGkey;

    [ObservableProperty]
    private string? _estId;

    [ObservableProperty]
    private string? _tenantGkey;

    [ObservableProperty]
    private decimal? _estlCgstPercent;

    [ObservableProperty]
    private decimal? _estlCgstAmount;

    [ObservableProperty]
    private decimal? _estlIgstPercent;

    [ObservableProperty]
    private decimal? _estlIgstAmount;

    [ObservableProperty]
    private decimal? _estlTotal;

    [ObservableProperty]
    private decimal? _estlSgstAmount;

    [ObservableProperty]
    private decimal? _estlSgstPercent;

    [ObservableProperty]
    private string? _productId;

    [ObservableProperty]
    private string? _metal;
}
}
