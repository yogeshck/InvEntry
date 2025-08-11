using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class CustomerOrderDBView : BaseEntity
    {

        [ObservableProperty]
        private int? custGkey;

        [ObservableProperty]
        private string? customerName;

        [ObservableProperty]
        private string? custMobileNbr;

        [ObservableProperty]
        private string? orderNbr;

        [ObservableProperty]
        private string? orderRefNbr;

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
        private string? orderStatus;

        [ObservableProperty]
        private int? orderBranch;

        [ObservableProperty]
        private decimal? advancePaidAmount;

        [ObservableProperty]
        private decimal? balanceAmount;

        [ObservableProperty]
        private int? orderLineNbr;

        [ObservableProperty]
        private string? prodCategory;

        [ObservableProperty]
        private decimal? prodGrossWeight;

        [ObservableProperty]
        private decimal? prodStoneWeight;

        [ObservableProperty]
        private decimal? prodNetWeight;

        [ObservableProperty]
        private int prodQty;
    }
}
