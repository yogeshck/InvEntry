using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Charts.Designer.Native;
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

public partial class OldMetalTransactionListViewModel: ObservableObject
{
    private readonly IOldMetalTransactionService _oldMetalTransService;
    private readonly IDialogService _reportDialogService;

    [ObservableProperty]
    private ObservableCollection<OldMetalTransaction> _oldMetalTransaction;

    [ObservableProperty]
    private DateSearchOption _searchOption;

    [ObservableProperty]
    private OldMetalTransaction _selectedTransaction;

    [ObservableProperty]
    private DateTime _Today = DateTime.Today;

    public OldMetalTransactionListViewModel (IOldMetalTransactionService oldMetalTransService, 
                                IDialogService reportDialogService
                                )
    {
        _oldMetalTransService = oldMetalTransService;
        _reportDialogService = reportDialogService;

        _searchOption = new();
        SearchOption.To = Today;
        SearchOption.From = Today.AddDays(-1);

        Task.Run(RefreshOldMetalTransAsync).Wait();

    }

    [RelayCommand]
    private async Task RefreshOldMetalTransAsync()
    {
        var oldMetalTransResult = await _oldMetalTransService.GetAll(SearchOption);

        if (oldMetalTransResult is not null)
            OldMetalTransaction = new(oldMetalTransResult);

    }

    [RelayCommand(CanExecute = nameof(CanPrintDeliveryNote))]
    private void PrintDeliveryNote()
    {
        _reportDialogService.PrintPreviewEstimate(SelectedTransaction.DocRefNbr, SelectedTransaction.GKey);

        //PrintPreviewEstimate(SelectedEstimate.EstNbr);
    }

    private bool CanPrintDeliveryNote()
    {
        return SelectedTransaction is not null;
    }

    [RelayCommand]
    private void SelectionChanged()
    {
        PrintDeliveryNoteCommand.NotifyCanExecuteChanged();
    }
}
