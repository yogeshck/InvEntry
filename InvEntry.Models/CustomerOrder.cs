using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace InvEntry.Models;

public partial class CustomerOrder : BaseEntity
{
    public CustomerOrder()
    {
        Lines = new();
        OldMetalTransactions = new();
        AdvanceReceiptLines = new();
    }

    [ObservableProperty]
    private string? invNbr;

    [ObservableProperty]
    private DateTime? invDate;

    [ObservableProperty]
    private int? custGkey;

    [ObservableProperty]
    private string? custMobileNbr;

    [ObservableProperty]
    private string? orderNbr;

    [ObservableProperty]
    private DateTime? orderDate;

    [ObservableProperty]
    private string? orderType;

    [ObservableProperty]
    private DateTime? orderDueDate;

    [ObservableProperty]
    private DateTime? deliveryDate;

    [ObservableProperty]
    private int? orderStatusFlag;

    [ObservableProperty]
    private int? orderBranch;

    [ObservableProperty]
    private int? serviceBranch;

    [ObservableProperty]
    private int? deliveryBranch;

    [ObservableProperty]
    private DateTime? orderTransferDate;

    [ObservableProperty]
    private string? baseMaterial;

    [ObservableProperty]
    private decimal? totalGrossWeight;

    [ObservableProperty]
    private decimal? totalStoneWeight;

    [ObservableProperty]
    private decimal? totalNetWeight;

    [ObservableProperty]
    private int? orderedItems;

    [ObservableProperty]
    private int? fulfilledItems;

    [ObservableProperty]
    private decimal? oldMetalNetWeight;

    [ObservableProperty]
    private decimal? oldMetalFineWeight;

    [ObservableProperty]
    private decimal? balanceWeight;

    [ObservableProperty]
    private decimal? metalRate;

    [ObservableProperty]
    private decimal? totalMakingCharges;

    [ObservableProperty]
    private decimal? totalTaxAmount;

    [ObservableProperty]
    private decimal? totalOrderAmount;

    [ObservableProperty]
    private decimal? advancePaidAmount;

    [ObservableProperty]
    private decimal? balanceAmount;

    [ObservableProperty]
    private string? remark;

    [ObservableProperty]
    private string? createdBy;

    [ObservableProperty]
    private DateTime? createdOn;

    [ObservableProperty]
    private string? modifiedBy;

    [ObservableProperty]
    private DateTime? modifiedOn;

    [ObservableProperty]
    private int? tenantGkey;

    [ObservableProperty]
    private string? orderRefNbr;

    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<CustomerOrderLine>? lines;

    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<OldMetalTransaction>? oldMetalTransactions;

    [ObservableProperty]
    [property: JsonIgnore]
    private ObservableCollection<LedgersTransactions>? advanceReceiptLines;


 //{ get; set; } = new();

}

