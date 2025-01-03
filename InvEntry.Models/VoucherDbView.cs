using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class VoucherDbView : BaseEntity
    {

        [ObservableProperty]
        public int? _seqNbr;

        [ObservableProperty]
        public int? _customerGkey;

        [ObservableProperty]
        public string? _transType;

        [ObservableProperty]
        public string? _voucherType;

        [ObservableProperty]
        public string? _mode;

        [ObservableProperty]
        public decimal? _transAmount;

        [ObservableProperty]
        public string? _voucherNbr;

        [ObservableProperty]
        public DateTime? _voucherDate;

        [ObservableProperty]
        public int? _refDocGkey;

        [ObservableProperty]
        public string? _refDocNbr;

        [ObservableProperty]
        public DateTime? _refDocDate;

        [ObservableProperty]
        public string? _transDesc;

        [ObservableProperty]
        public DateTime? _transDate;

        [ObservableProperty]
        public decimal? _recdAmount;

        [ObservableProperty]
        public decimal? _paidAmount;

        [ObservableProperty]
        public int? _fromLedgerGkey;

        [ObservableProperty]
        public string? _fromLedgerName;

        [ObservableProperty]
        public int? _toLedgerGkey;

        [ObservableProperty]
        public string? _toLedgerName;

        [ObservableProperty]
        public decimal? _obAmount;

        [ObservableProperty]
        public decimal? _cbAmount;

        [ObservableProperty]
        public int? _fundTransferMode;

        [ObservableProperty]
        public int? _fundTransferRefGkey;

        [ObservableProperty]
        public DateTime? _fundTransferDate;
    }

}

