using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Tern.MI.InvEntry.Models;

public partial class InvoiceLine : BaseEntity
{
    [ObservableProperty]
    private InvoiceHeader invHeader; // invoice Header Gkey;

    [ObservableProperty]
    private long invHeaderId;

    [ObservableProperty]

    private int invLineNbr;

    [ObservableProperty]
    private string? hsnCode;

    [ObservableProperty]
    private long? productGkey;

    [ObservableProperty]
    private string? productName;

    [ObservableProperty]
    private string? productDesc;

    [ObservableProperty]
    private string? productPurity;

    [ObservableProperty]
    private string? prodCategory;

    [ObservableProperty]
    private int prodQty;

    [ObservableProperty]
    private double? prodGrossWeight;

    [ObservableProperty]
    private double? prodStoneWeight;

    [ObservableProperty]
    private double? prodNetWeight;

    [ObservableProperty]
    private double? vaPercent;

    [ObservableProperty]
    private double? vaAmount;

    [ObservableProperty]
    private double? invlBilledPrice;

    [ObservableProperty]
    private double? invlGrossAmt;

    [ObservableProperty]
    private double? invlWastageAmt;

    [ObservableProperty]
    private double? invlMakingCharges;

    [ObservableProperty]
    private double? invlStoneAmt;

    [ObservableProperty]
    private double? invlTaxableAmount;

    [ObservableProperty]
    private double? invlOtherCharges;

    [ObservableProperty]
    private bool? isTaxable;

    [ObservableProperty]
    private string? taxType;

    [ObservableProperty]
    private double? taxPercent;

    [ObservableProperty]
    private double? taxAmount;

    [ObservableProperty]
    private double? invlPayableAmt;

    [ObservableProperty]
    private string? itemNotes;

    [ObservableProperty]
    private string? invNote;

    [ObservableProperty]
    private bool itemPacked = false;

    [ObservableProperty]
    private string? productPackCode;
}
