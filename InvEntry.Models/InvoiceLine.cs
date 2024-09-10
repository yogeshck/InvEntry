using CommunityToolkit.Mvvm.ComponentModel;

namespace InvEntry.Models;

public partial class InvoiceLine : BaseEntity
{
    [ObservableProperty]
    private decimal? _gkey;

    [ObservableProperty]
    private string? _hsnCode;

    [ObservableProperty]
    private long? _invLineNbr;

    [ObservableProperty]
    private string? _invNote;

    [ObservableProperty]
    private double? _invlBilledPrice;

    [ObservableProperty]
    private double? _invlGrossAmt;

    [ObservableProperty]
    private double? _invlMakingCharges;

    [ObservableProperty]
    private double? _invlOtherCharges;

    [ObservableProperty]
    private double? _invlPayableAmt;

    [ObservableProperty]
    private double? _invlStoneAmount;

    [ObservableProperty]
    private double? _invlTaxableAmount;

    [ObservableProperty]
    private double? _invlWastageAmt;

    [ObservableProperty]
    private short? _isTaxable;

    [ObservableProperty]
    private string? _itemNotes;

    [ObservableProperty]
    private short? _itemPacked;

    [ObservableProperty]
    private string? _prodCategory;

    [ObservableProperty]
    private double? _prodGrossWeight;

    [ObservableProperty]
    private double? _prodNetWeight;

    [ObservableProperty]
    private long? _prodQty;

    [ObservableProperty]
    private double? _prodStoneWeight;

    [ObservableProperty]
    private string? _productDesc;

    [ObservableProperty]
    private decimal? _productGkey;

    [ObservableProperty]
    private string? _productName;

    [ObservableProperty]
    private string? _prodPackCode;

    [ObservableProperty]
    private string? _productPurity;

    [ObservableProperty]
    private double? _taxAmount;

    [ObservableProperty]
    private double? _taxPercent;

    [ObservableProperty]
    private string? _taxType;

    [ObservableProperty]
    private double? _vaAmount;

    [ObservableProperty]
    private double? _vaPercent;

    [ObservableProperty]
    private decimal? _invoiceHdrGkey;

    [ObservableProperty]
    private decimal? _invoiceId;

    [ObservableProperty]
    private byte[]? _tenantGkey;
}
