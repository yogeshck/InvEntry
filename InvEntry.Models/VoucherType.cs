using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class VoucherType : BaseEntity
    {
        [ObservableProperty]
        public string? _documentType;

        [ObservableProperty]
        public string? _abbreviation;

        [ObservableProperty]
        private int? _mtblVoucherTypeGkey;

        [ObservableProperty]
        public int? _lastUsedNumber;

        [ObservableProperty]
        public bool? _isTaxable;

        [ObservableProperty]
        public string? _narration;

        [ObservableProperty]
        public string? _docNbrMethod;

        [ObservableProperty]
        public int? _docNbrLength;

        [ObservableProperty]
        public string? _docNbrPrefill;

        [ObservableProperty]
        public string? _docNbrPrefix;

        [ObservableProperty]
        public string? _docNbrSuffix;

        [ObservableProperty]
        public bool? _isActive;

        [ObservableProperty]
        public string? _usedFor;
    }

}
