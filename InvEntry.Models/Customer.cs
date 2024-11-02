using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class Customer : BaseEntity
    {
        public Customer()
        {
            address = new();
        }

        [ObservableProperty]
        [property:JsonIgnore]
        private OrgAddress address;

        [ObservableProperty]
        private string clientId;

        [ObservableProperty]
        private string mobileNbr;

        [ObservableProperty]
        private string customerName;

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string ledgerName;

        [ObservableProperty]
        private string gstStateCode;

        [ObservableProperty]
        private string customerType;

        [ObservableProperty]
        private string gstinNbr; 

        [ObservableProperty]
        private string notes; 

        [ObservableProperty]
        private string panNbr; 

        [ObservableProperty]
        private string status; 

        [ObservableProperty]
        private string creditAvailed; 

        [ObservableProperty]
        private int? locationGkey; 

        [ObservableProperty]
        private bool? deleteFlag; 

        [ObservableProperty]
        private int? tenantGkey;

        [ObservableProperty]
        private int? addressGkey;

        [ObservableProperty]
        private DateTime? customerSince; 

        [ObservableProperty]
        private string? salutations;


    }
}
