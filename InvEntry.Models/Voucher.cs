﻿using CommunityToolkit.Mvvm.ComponentModel;
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
        private int? _SeqNbr ;

        [ObservableProperty]
        private long? _CustomerGkey;

        [ObservableProperty]
        private bool? _TransType;

        [ObservableProperty]
        private bool? _VoucherType;

        [ObservableProperty]
        private bool? _Mode;

        [ObservableProperty]
        private decimal? _TransAmount;

        [ObservableProperty]
        private string? _VoucherNbr;

        [ObservableProperty]
        private DateTime? _VoucherDate;

        [ObservableProperty]
        private long? _RefDocGkey;

        [ObservableProperty]
        private string? _RefDocNbr;

        [ObservableProperty]
        private DateTime? _RefDocDate;

        [ObservableProperty]
        private string? _TransDesc;

        [ObservableProperty]
        private DateTime? _TransDate;

        [ObservableProperty]
        private int? _FromLedgerGkey;

        [ObservableProperty]
        private int? _ToKedgerGkey;

        [ObservableProperty]
        private decimal? _ObAmount;

        [ObservableProperty]
        private decimal? _CbAmount;

        [ObservableProperty]
        private bool? _FundTransferMode;

        [ObservableProperty]
        private int? _FundTransferRefGkey;

        [ObservableProperty]
        private DateTime? _FundTransferDate;
    }
}
