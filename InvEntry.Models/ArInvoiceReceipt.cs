using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Models
{
     public partial class ArInvoiceReceipt : BaseEntity
    {
        [ObservableProperty]
        public int? _custGkey;

        [ObservableProperty]
        public int? _invoiceGkey;

        [ObservableProperty]
        public string? _invoiceNbr;

        [ObservableProperty]
        public decimal? _invoiceReceivableAmount;

        [ObservableProperty]
        public decimal? _balanceAfterAdj;

        [ObservableProperty]
        public int _seqNbr;

        [ObservableProperty]
        public string? _transactionType;

        [ObservableProperty]
        public string? _modeOfReceipt;

        [ObservableProperty]
        public decimal? _balBeforeAdj;

        [ObservableProperty]
        public decimal? _adjustedAmount;

        [ObservableProperty]
        public string? _internalVoucherNbr;

        [ObservableProperty]
        public DateTime? _internalVoucherDate;

        [ObservableProperty]
        public string? _externalTransactionId;

        [ObservableProperty]
        public DateTime? _externalTransactionDate;

        [ObservableProperty]
        public string? _bankName;

        [ObservableProperty]
        public string? _otherReference;

        [ObservableProperty]
        public string? _senderBankAccountNbr;

        [ObservableProperty]
        public int? _senderBankGkey;

        [ObservableProperty]
        public string? _senderBankBranch;

        [ObservableProperty]
        public string? _senderBankIfscCode;

        [ObservableProperty]
        public string? _companyBankAccountNbr;

        [ObservableProperty]
        public byte? _status;
    }

}
