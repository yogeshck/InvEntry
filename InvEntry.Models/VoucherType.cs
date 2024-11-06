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
        public string? _DocumentType;

        [ObservableProperty]
        public string? _Abbreviation;

        [ObservableProperty]
        private int? _MtblVoucherTypeGkey;

        [ObservableProperty]
        public int? _LastUsedNumber;

        [ObservableProperty]
        public bool? _IsTaxable;

        [ObservableProperty]
        public string? _Narration;

        [ObservableProperty]
        public string? _DocNbrMethod;

        [ObservableProperty]
        public int? _DocNbrLength;

        [ObservableProperty]
        public string? _DocNbrPrefill;

        [ObservableProperty]
        public string? _DocNbrPrefix;

        [ObservableProperty]
        public string? _DocNbrSuffix;

        [ObservableProperty]
        public bool? _IsActive;

        [ObservableProperty]
        public string? _UsedFor;
    }

}
