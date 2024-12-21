using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public partial class EstimateListViewModel : ObservableObject
{

    private readonly IEstimateService _estimateService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<EstimateHeader> _estimates;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private EstimateHeader _SelectedEstimate;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public EstimateListViewModel(IEstimateService estimateService,
    [FromKeyedServices("ReportDialogService")] IDialogService reportDialogService)
    {
        _estimateService = estimateService;
        _reportDialogService = reportDialogService;
        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-1);

        Task.Run(RefreshEstimateAsync).Wait();
    }

    [RelayCommand]
    private async Task RefreshEstimateAsync()
    {
        var estimateResult = await _estimateService.GetAll(SearchOption);
        if (estimateResult is not null)
            Estimates = new(estimateResult);
    }

    [RelayCommand(CanExecute = nameof(CanPrintEstimate))]
    private void PrintEstimate()
    {
        _reportDialogService.PrintPreview(SelectedEstimate.EstNbr);
    }

    private bool CanPrintEstimate()
    {
        return SelectedEstimate is not null;
    }

    [RelayCommand]
    private void SelectionChanged()
    {
        PrintEstimateCommand.NotifyCanExecuteChanged();
    }


}
