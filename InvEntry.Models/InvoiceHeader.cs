﻿using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace InvEntry.Models;

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
    private decimal? invlTaxableAmount;

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
    private decimal? invBalance;

    [ObservableProperty]
    private decimal? invRefund;

    [ObservableProperty]
    private string? invNotes;

    [ObservableProperty]
    private bool? isTaxApplicable;

    [ObservableProperty]
    private string? taxType;      // GST

    [ObservableProperty]
    private decimal? cgstPercentage;

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
    private decimal? invlTaxTotal;

    [ObservableProperty]
    private string? tenantGkey;

    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<InvoiceLine>? lines;

}
