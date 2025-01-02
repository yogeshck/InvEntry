using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class MtblLedger : BaseEntity
    {

        [ObservableProperty]
        public string? _ledgerName;

        [ObservableProperty]
        public string? _description;

        [ObservableProperty]
        public string? _accountGroupName;

        [ObservableProperty]
        public bool? _status;

        [ObservableProperty]
        public int? _subGroup;

        [ObservableProperty]
        public int? _primaryGroup;

        [ObservableProperty]
        public int? _memberGkey;

        [ObservableProperty]
        public string? _memberType;

        [ObservableProperty]
        public int? _tenantGkey;

        [ObservableProperty]
        public int? _ledgerAccountCode;

        [ObservableProperty]
        public string? _transactionType;

    }
}
