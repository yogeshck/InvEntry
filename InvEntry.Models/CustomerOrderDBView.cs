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
        public int? custGkey;

        [ObservableProperty]
        public string? customerName;

        [ObservableProperty]
        public string? custMobileNbr;

        [ObservableProperty]
        public string? orderNbr;

        [ObservableProperty]
        public DateTime? orderDate;

        [ObservableProperty]
        public string? orderType;

        [ObservableProperty]
        public DateTime? orderDueDate;

        [ObservableProperty]
        public DateTime? deliveryDate;

        [ObservableProperty]
        public int? orderStatusFlag;

        [ObservableProperty]
        public string? orderStatus;

        [ObservableProperty]
        public int? orderBranch;

        [ObservableProperty]
        public decimal? advancePaidAmount;

        [ObservableProperty]
        public decimal? balanceAmount;

        [ObservableProperty]
        public int? orderLineNbr;

        [ObservableProperty]
        public string? prodCategory;

        [ObservableProperty]
        public decimal? prodGrossWeight;

        [ObservableProperty]
        public decimal? prodStoneWeight;

        [ObservableProperty]
        public decimal? prodNetWeight;

        [ObservableProperty]
        public int prodQty;
    }
}
