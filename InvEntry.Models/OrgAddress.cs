using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class OrgAddress : BaseEntity
    {
        [ObservableProperty]
        private string? _AddressLine1;

        [ObservableProperty]
        private string? _AddressLine2;

        [ObservableProperty]
        private string? _AddressLine3;

        [ObservableProperty]
        private string? _City;

        [ObservableProperty]
        private string? _District;

        [ObservableProperty]
        private string? _State;

        [ObservableProperty]
        private string? _Country;

        [ObservableProperty]
        private decimal? _TenantGkey;

        [ObservableProperty]
        private string? _Area;

        [ObservableProperty]
        private string? _GstStateCode;
    }
}
