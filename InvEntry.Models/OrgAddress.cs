﻿using CommunityToolkit.Mvvm.ComponentModel;
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
        private string? _addressLine1;

        [ObservableProperty]
        private string? _addressLine2;

        [ObservableProperty]
        private string? _addressLine3;

        [ObservableProperty]
        private string? _city;

        [ObservableProperty]
        private string? _district;

        [ObservableProperty]
        private string? _state;

        [ObservableProperty]
        private string? _country;

        [ObservableProperty]
        private decimal? _tenantGkey;

        [ObservableProperty]
        private string? _area;

        [ObservableProperty]
        private string? _gstStateCode;
    }
}
