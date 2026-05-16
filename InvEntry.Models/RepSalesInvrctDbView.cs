using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace InvEntry.Models;

public partial class RepSalesInvrctDbView : BaseEntity
{

    [ObservableProperty]
    public string? _invNbr;

    [ObservableProperty]
    public DateTime? _invDate;

    [ObservableProperty]
    public decimal? _invAmount;

    [ObservableProperty]
    public decimal? _advanceAmt;

    [ObservableProperty]
    public decimal? _discountAmt;

    [ObservableProperty]
    public decimal? _refund;

    [ObservableProperty]
    public decimal? _rd;

    [ObservableProperty]
    public decimal? _cash;

    [ObservableProperty]
    public decimal? _gpay;

    [ObservableProperty]
    public decimal? _creditCard;

    [ObservableProperty]
    public decimal? _debitCard;

    [ObservableProperty]
    public decimal? _bank;

    [ObservableProperty]
    public decimal? _credit;
}
