using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Tally;
using InvEntry.Tally.Model;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevExpress.Xpf.Printing;
using IDialogService = DevExpress.Mvvm.IDialogService;
using InvEntry.Reports;
using System.Windows;
using System.Collections.Generic;
using DevExpress.XtraPivotGrid.Data;
using System.Windows.Documents;
using DevExpress.Mvvm.Native;
using System.Linq;
using DevExpress.XtraGrid.Views.Items;

namespace InvEntry.ViewModels;


public partial class GRNListViewModel : ObservableObject
{

    private readonly IGrnDbViewService _grnDbViewService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<GrnDbView> _grnDbView;

    [ObservableProperty]
    private DateSearchOption _dateSearchOption;

    [ObservableProperty]
    private GrnHeader _SelectedGRN;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public GRNListViewModel(IGrnDbViewService grnDbViewService,
                             [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService )  
    {
        _grnDbViewService = grnDbViewService;
        _reportDialogService = reportDialogService;
        _dateSearchOption = new();
        DateSearchOption.To = Today;
        DateSearchOption.From = Today.AddDays(-1);

        Task.Run(RefreshGrnHeader).Wait();

    }

    [RelayCommand]
    private async Task RefreshGrnHeader() 
    {
        var grnResult = await _grnDbViewService.GetAll(DateSearchOption);

        if (grnResult is not null)

            GrnDbView = new (grnResult);

    }

}
