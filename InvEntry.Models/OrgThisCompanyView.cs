using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{

    public partial class OrgThisCompanyView : BaseEntity
    {
        [ObservableProperty]
        private string? _companyName;

        [ObservableProperty]
        private string? _addressLine1;

        [ObservableProperty]
        private string? _addressLine2;

        [ObservableProperty]
        private string? _addressLine3;

        [ObservableProperty]
        private string? _area;

        [ObservableProperty]
        private string? _state;

        [ObservableProperty]
        private string? _gstCode;

        [ObservableProperty]
        private string? _country;

        [ObservableProperty]
        private string? _district;

        [ObservableProperty]
        private string? _city;

        [ObservableProperty]
        private int? _pincode;

        [ObservableProperty]
        private string? _panNbr;

        [ObservableProperty]
        private string? _gtNbr;

        [ObservableProperty]
        private bool _thisCompany;

        [ObservableProperty]
        public string? _contactNbr1;

        [ObservableProperty]
        public string? _contactNbr2;

        [ObservableProperty]
        public string? _emailId;

        [ObservableProperty]
        public int _tenantGkey;

        [ObservableProperty]
        public string? _Tagline;

        [ObservableProperty]
        public string? _ServiceTaxNbr;

    }

}
