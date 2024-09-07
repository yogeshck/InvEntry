using CommunityToolkit.Mvvm.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Tern.MI.InvEntry.Models;

public partial class InvoiceHeader : BaseEntity
{

    [ObservableProperty]

    private string? invNbr;

    [ObservableProperty]

    private DateTime? invDate;

    [ObservableProperty]

    private string? invCustMobile;

    [ObservableProperty]

    private string? placeOfSeller;          // Default to seller location GST code if TN - 33

    [ObservableProperty]

    private string? placeOfSupply;           // Based on goods / service receiver

    [ObservableProperty]

    private DateTime? pymtDueDate;

    [ObservableProperty]
    private double? invlTaxableAmount;

    [ObservableProperty]
    private double? advanceAdj;

    [ObservableProperty]
    private double? rdAmountAdj;

    [ObservableProperty]
    private double? oldGoldAmount;

    [ObservableProperty]
    private double? oldSilverAmount;

    [ObservableProperty]
    private double? discountPercent;

    [ObservableProperty]
    private double? discountAmount;

    [ObservableProperty]
    private double? roundOff;

    [ObservableProperty]
    private double? amountPayable;

    [ObservableProperty]
    private double? recdAmount;

    [ObservableProperty]
    private double? invBalance;

    [ObservableProperty]
    private double? invRefund;

    [ObservableProperty]
    private string? invNotes;

    [ObservableProperty]
    private bool? isTaxApplicable;

    [ObservableProperty]
    private string? taxType;      // GST

    [ObservableProperty]
    private double? cgstPercentage;

    [ObservableProperty]
    private double? sgstPercent;

    [ObservableProperty]
    private double? igstPercent;

    [ObservableProperty]
    private double? cgstAmount;

    [ObservableProperty]
    private double? sgstAmount;

    [ObservableProperty]
    private double? igstAmount;

    [ObservableProperty]
    private string? paymentMode;

    [ObservableProperty]
    private List<InvoiceLine>? lines;

}
