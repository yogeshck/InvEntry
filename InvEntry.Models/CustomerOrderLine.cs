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
    public string? orderNbr;

    [ObservableProperty]
    public int? orderGkey;

    [ObservableProperty]
    public int? orderLineNbr;

    [ObservableProperty]
    public string? prodCategory;

    [ObservableProperty]
    public int? productGkey;

    [ObservableProperty]
    private string? _productId;

    [ObservableProperty]
    public string? _productSku;

    [ObservableProperty]
    public string? productName;

    [ObservableProperty]
    public string? productDesc;

    [ObservableProperty]
    public string? productMetal;

    [ObservableProperty]
    public string? productPurity;

    [ObservableProperty]
    public string? orderSpecification;

    [ObservableProperty]
    public int prodQty;

    [ObservableProperty]
    public decimal? prodGrossWeight;

    [ObservableProperty]
    public decimal? prodStoneWeight;

    [ObservableProperty]
    public decimal? prodNetWeight;

    [ObservableProperty]
    public string? orderType;

    [ObservableProperty]
    public string? itemNotes;

    [ObservableProperty]
    public bool? itemPacked;

    [ObservableProperty]
    public DateTime? orderItemDueDate;

    [ObservableProperty]
    public DateTime? deliveryDate;

    [ObservableProperty]
    public int? orderItemStatusFlag;

    [ObservableProperty]
    public int? orderBranch;

    [ObservableProperty]
    public int? serviceBranch;

    [ObservableProperty]
    public int? deliveryBranch;

    [ObservableProperty]
    public DateTime? orderTransferDate;

    [ObservableProperty]
    public decimal? totalGrossWeight;

    [ObservableProperty]
    public decimal? totalStoneWeight;

    [ObservableProperty]
    public decimal? totalNetWeight;

    [ObservableProperty]
    public int? orderedItems;
    
    [ObservableProperty]
    public int? fulfilledItems;

    [ObservableProperty]
    public decimal? oldMetalNetWeight;

    [ObservableProperty]
    public decimal? oldMetalFinePercent;

    [ObservableProperty]
    public decimal? oldMetalFineWeight;

    [ObservableProperty]
    public decimal? balanceWeight;

    [ObservableProperty]
    public decimal? metalRate;

    [ObservableProperty]
    public decimal? makingCharges;

    [ObservableProperty]
    public decimal? vaPercent;

    [ObservableProperty]
    public decimal? vaAmount;

    [ObservableProperty]
    public decimal? taxAmount;

    [ObservableProperty]
    public decimal? orderAmount;

    [ObservableProperty]
    public decimal? advancePaidAmount;

    [ObservableProperty]
    public decimal? balanceAmount;

    [ObservableProperty]
    public string? remark;

    [ObservableProperty]
    public int? catalogId;

    [ObservableProperty]
    public string? designName;

    [ObservableProperty]
    public int? pageNbr;

    [ObservableProperty]
    public string? imageName;

    [ObservableProperty]
    public string? imagePath;

    [ObservableProperty]
    public string? createdBy;

    [ObservableProperty]
    public DateTime? createdOn;

    [ObservableProperty]
    public string? modifiedBy;

    [ObservableProperty]
    public DateTime? modifiedOn;

    [ObservableProperty]
    public int? tenantGkey;
}

