using CommunityToolkit.Mvvm.ComponentModel;

namespace InvEntry.Models;

public partial class InvoiceLine : BaseEntity
{
    [ObservableProperty]
    private string? _hsnCode;

    [ObservableProperty]
    private int? _invLineNbr;

    [ObservableProperty]
    private string? _invNote;

    [ObservableProperty]
    private decimal? _invlBilledPrice;

    [ObservableProperty]
    private decimal? _invlGrossAmt;

    [ObservableProperty]
    private decimal? _invlMakingCharges;

    [ObservableProperty]
    private decimal? _invlOtherCharges;

    [ObservableProperty]
    private decimal? _invlPayableAmt;

    [ObservableProperty]
    private decimal? _invlStoneAmount;

    [ObservableProperty]
    private decimal? _invlTaxableAmount;

    [ObservableProperty]
    private decimal? _invlWastageAmt;

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
    private decimal? _invoiceHdrGkey;

    [ObservableProperty]
    private string? _invoiceId;

    [ObservableProperty]
    private string? _tenantGkey;

    [ObservableProperty]
    private decimal? _invlCgstPercent;

    [ObservableProperty]
    private decimal? _invlCgstAmount;

    [ObservableProperty]
    private decimal? _invlIgstPercent;

    [ObservableProperty]
    private decimal? _invlIgstAmount;

    [ObservableProperty]
    private decimal? _invlTotal;

    [ObservableProperty]
    private decimal? _invlSgstAmount;

    [ObservableProperty]
    private decimal? _invlSgstPercent;

    [ObservableProperty]
    private string? _productId;
}
