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
        private string _clientId;

        [ObservableProperty]
        public string? _customerName;

        [ObservableProperty]
        public string? _customerType;

        [ObservableProperty]
        public string? _mobileNbr;

        [ObservableProperty]
        public string? _gstinNbr;

        [ObservableProperty]
        public string? _ledgerName;

        [ObservableProperty]
        public string? _notes;

        [ObservableProperty]
        public string? _panNbr;

        [ObservableProperty]
        public string? _status;

        [ObservableProperty]
        public string? _creditAvailed;

        [ObservableProperty]
        public int? _locationGkey;

        [ObservableProperty]
        public bool? _deleteFlag;

        [ObservableProperty]
        public int? _tenantGkey;

        [ObservableProperty]
        public int? _addressGkey;

        [ObservableProperty]
        public DateTime? _customerSince;

        [ObservableProperty]
        public string? _salutations;

        [ObservableProperty]
        public string? _gstStateCode;
    }
}
