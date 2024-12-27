using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
    public partial class Voucher : BaseEntity
    {

        [ObservableProperty]
        private int? _seqNbr ;

        [ObservableProperty]
        private int? _customerGkey;

        [ObservableProperty]
        private string? _transType;

        [ObservableProperty]
        private string? _voucherType;

        [ObservableProperty]
        private string? _mode;

        [ObservableProperty]
        private decimal? _transAmount;

        [ObservableProperty]
        private string? _voucherNbr;

        [ObservableProperty]
        private DateTime? _voucherDate;

        [ObservableProperty]
        private long? _refDocGkey;

        [ObservableProperty]
        private string? _refDocNbr;

        [ObservableProperty]
        private DateTime? _refDocDate;

        [ObservableProperty]
        private string? _transDesc;

        [ObservableProperty]
        private DateTime? _transDate;

        [ObservableProperty]
        private int? _fromLedgerGkey;

        [ObservableProperty]
        private int? _toLedgerGkey;

        [ObservableProperty]
        private decimal? _obAmount;

        [ObservableProperty]
        private decimal? _cbAmount;

        [ObservableProperty]
        private int? _fundTransferMode;

        [ObservableProperty]
        private int? _fundTransferRefGkey;

        [ObservableProperty]
        private DateTime? _fundTransferDate;

        [ObservableProperty]
        private decimal? _recdAmount;

        [ObservableProperty]
        private decimal? _paidAmount;
    }
}
