using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;


public partial class GRNListViewModel : ObservableObject
{

    private readonly IGrnService _grnService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<GrnHeader> _grnHeader;

    [ObservableProperty]
    private DateSearchOption _dateSearchOption;

    [ObservableProperty]
    private GrnHeader _SelectedGRN;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public GRNListViewModel( IGrnService grnService,
                             [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService )  
    {
        _grnService = grnService;
        _reportDialogService = reportDialogService;
        _dateSearchOption = new();
        DateSearchOption.To = Today;
        DateSearchOption.From = Today.AddDays(-1);

        Task.Run(RefreshGrnHeader).Wait();

    }

    [RelayCommand]
    private async Task RefreshGrnHeader() 
    {
        var grnResult = await _grnService.GetAll(DateSearchOption);

        if (grnResult is not null)

            GrnHeader = new (grnResult);

    }


}
