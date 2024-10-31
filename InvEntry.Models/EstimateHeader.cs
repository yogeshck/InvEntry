using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace InvEntry.Models;

public partial class EstimateHeader : BaseEntity
{
    public EstimateHeader()
    {
        Lines = new();
        PaymentMode = "EST_ONLY";
        TaxType = "GST";
        RoundOff = 0M;
        EstlTaxTotal = 0M;
        EstlTaxableAmount = 0M;
        DiscountAmount = 0M;
        OldGoldAmount = 0M;
        OldSilverAmount = 0M;
        AmountPayable = 0M;
        AdvanceAdj = 0M;
        RdAmountAdj = 0M;
        RecdAmount = 0M;
    }
    /*
        [ObservableProperty]
        private int gkey;*/

    [ObservableProperty]
    private string? estNbr;

    [ObservableProperty]
    private DateTime? estDate;

    [ObservableProperty]
    private string? custMobile;

    [ObservableProperty]
    private string? placeOfSeller;          // Default to seller location GST code if TN - 33

    [ObservableProperty]
    private string? placeOfSupply;           // Based on goods / service receiver

    [ObservableProperty]
    private DateTime? paymentDueDate;

    [ObservableProperty]
    private decimal? estlTaxableAmount;

    [ObservableProperty]
    private decimal? advanceAdj;

    [ObservableProperty]
    private decimal? rdAmountAdj;

    [ObservableProperty]
    private decimal? oldGoldAmount;

    [ObservableProperty]
    private decimal? oldSilverAmount;

    [ObservableProperty]
    private decimal? discountPercent;

    [ObservableProperty]
    private decimal? discountAmount;

    [ObservableProperty]
    private decimal? roundOff;

    [ObservableProperty]
    private decimal? amountPayable;

    [ObservableProperty]
    private decimal? recdAmount;

    [ObservableProperty]
    private decimal? estBalance;

    [ObservableProperty]
    private decimal? estRefund;

    [ObservableProperty]
    private string? estNotes;

    [ObservableProperty]
    private bool isTaxApplicable;

    [ObservableProperty]
    private string? taxType;      // GST

    [ObservableProperty]
    private decimal? cgstPercent;

    [ObservableProperty]
    private decimal? sgstPercent;

    [ObservableProperty]
    private decimal? igstPercent;

    [ObservableProperty]
    private decimal? cgstAmount;

    [ObservableProperty]
    private decimal? sgstAmount;

    [ObservableProperty]
    private decimal? igstAmount;

    [ObservableProperty]
    private string? paymentMode;

    [ObservableProperty]
    private decimal? grossRcbAmount;

    [ObservableProperty]
    private decimal? estlTaxTotal;

    [ObservableProperty]
    private int? tenantGkey;

    [ObservableProperty]
    private int? custGkey;

    [ObservableProperty]
    private string? gstLocSeller;

    [ObservableProperty]
    private string? gstLocBuyer;

    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<EstimateLine>? lines;

}
