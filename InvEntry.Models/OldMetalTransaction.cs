﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace InvEntry.Models
{
    public partial class OldMetalTransaction: BaseEntity
    {

        [ObservableProperty]
        public string? _transNbr;

        [ObservableProperty]
        public DateTime? _transDate;

        [ObservableProperty]
        public string? _transType;

        [ObservableProperty]
        public int? _docRefGkey;

        [ObservableProperty]
        public string? _docRefNbr;

        [ObservableProperty]
        public DateTime? _docRefDate;

        [ObservableProperty]
        public int? _custGkey;

        [ObservableProperty]
        public string? _custMobile;

        [ObservableProperty]
        public int? _productGkey;

        [ObservableProperty]
        public string? _productId;

        [ObservableProperty]
        public string? _productCategory;

        [ObservableProperty]
        public string? _metal;

        [ObservableProperty]
        public string? _purity;

        [ObservableProperty]
        public decimal? _transactedRate;

        [ObservableProperty]
        public string? _uom;

        [ObservableProperty]
        public decimal? _grossWeight;

        [ObservableProperty]
        public decimal? _stoneWeight;

        [ObservableProperty]
        public decimal? _wastagePercent;

        [ObservableProperty]
        public decimal? _wastageWeight;

        [ObservableProperty]
        public decimal? _netWeight;

        [ObservableProperty]
        public decimal? _totalProposedPrice;

        [ObservableProperty]
        public decimal? _finalPurchasePrice;

        [ObservableProperty]
        public string? _remarks;

        [ObservableProperty]
        public string? _docRefType;

    }
}

