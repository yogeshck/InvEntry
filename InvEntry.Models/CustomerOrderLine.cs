using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models;

public partial class CustomerOrderLine : BaseEntity
{

    [ObservableProperty]
    private string? orderNbr;

    [ObservableProperty]
    private int? orderGkey;

    [ObservableProperty]
    private int? orderLineNbr;

    [ObservableProperty]
    private string? prodCategory;

    [ObservableProperty]
    private int? productGkey;

    [ObservableProperty]
    private string? productId;

    [ObservableProperty]
    private string? productSku;

    [ObservableProperty]
    private string? productName;

    [ObservableProperty]
    private string? productDesc;

    [ObservableProperty]
    private string? productMetal;

    [ObservableProperty]
    private string? productPurity;

    [ObservableProperty]
    private string? orderSpecification;

    [ObservableProperty]
    private int prodQty;

    [ObservableProperty]
    private decimal? prodGrossWeight;

    [ObservableProperty]
    private decimal? prodStoneWeight;

    [ObservableProperty]
    private decimal? prodNetWeight;

    [ObservableProperty]
    private string? orderType;

    [ObservableProperty]
    private string? itemNotes;

    [ObservableProperty]
    private bool? itemPacked;

    [ObservableProperty]
    private DateTime? orderItemDueDate;

    [ObservableProperty]
    private DateTime? deliveryDate;

    [ObservableProperty]
    private int? orderItemStatusFlag;

    [ObservableProperty]
    private int? orderBranch;

    [ObservableProperty]
    private int? serviceBranch;

    [ObservableProperty]
    private int? deliveryBranch;

    [ObservableProperty]
    private DateTime? orderTransferDate;

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
    private decimal? oldMetalFinePercent;

    [ObservableProperty]
    private decimal? oldMetalFineWeight;

    [ObservableProperty]
    private decimal? balanceWeight;

    [ObservableProperty]
    private decimal? metalRate;

    [ObservableProperty]
    private decimal? makingCharges;

    [ObservableProperty]
    private decimal? vaPercent;

    [ObservableProperty]
    private decimal? vaAmount;

    [ObservableProperty]
    private decimal? taxAmount;

    [ObservableProperty]
    private decimal? orderAmount;

    [ObservableProperty]
    private decimal? advancePaidAmount;

    [ObservableProperty]
    private decimal? balanceAmount;

    [ObservableProperty]
    private string? remark;

    [ObservableProperty]
    private int? catalogId;

    [ObservableProperty]
    private string? designName;

    [ObservableProperty]
    private int? pageNbr;

    [ObservableProperty]
    private string? imageName;

    [ObservableProperty]
    private string? imagePath;

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
}

