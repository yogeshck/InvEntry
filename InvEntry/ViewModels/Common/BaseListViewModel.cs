using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models.UI;
using System.Collections.ObjectModel;

namespace InvEntry.ViewModels.Common;

public abstract partial class BaseListViewModel<T>
    : ObservableObject
    where T : class
{
    // ============================================================
    // DATA
    // ============================================================

    [ObservableProperty]
    private ObservableCollection<T> items = new();

    [ObservableProperty]
    private T? selectedItem;


    // ============================================================
    // SEARCH
    // ============================================================

    [ObservableProperty]
    private ListSearchOption searchOption = new();


    // ============================================================
    // UI STATE
    // ============================================================

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage =
        string.Empty;


    // ============================================================
    // CONFIGURATION
    // ============================================================

    public ObservableCollection<ListColumnDefinition> Columns
    {
        get;
    } = new();


    public ListFilterDefinition FilterDefinition
    {
        get;
        protected set;
    } = new();


    public virtual string Title =>
        "LIST";


    public virtual string Description =>
        string.Empty;


    public virtual bool SupportsDocumentPrint =>
        false;


    public DateTime Today =>
        DateTime.Today;

    public virtual bool CanExportExcel =>
    false;

    public virtual bool CanExportPdf =>
        false;

    public virtual bool CanBulkPrint =>
        false;

    // ============================================================
    // FILTER VISIBILITY
    // ============================================================

    public bool HasAdditionalFilter =>
        FilterDefinition.Type !=
        ListFilterType.None;


    public bool IsTextFilter =>
        FilterDefinition.Type ==
        ListFilterType.Text;


    public bool IsSelectionFilter =>
        FilterDefinition.Type ==
        ListFilterType.Selection;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    protected BaseListViewModel()
    {
        SearchOption.From =
            DateTime.Today.AddDays(-1);

        SearchOption.To =
            DateTime.Today;
    }


    // ============================================================
    // INITIALIZE
    // ============================================================

    [RelayCommand]
    private async Task InitializeAsync()
    {
        await RefreshAsync();
    }


    // ============================================================
    // REFRESH
    // ============================================================

    private bool CanRefresh()
    {
        return !IsBusy;
    }


    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;

            StatusMessage = "Loading records...";

            SelectedItem = null;

            var result =
                await LoadItemsAsync(SearchOption);

            Items.Clear();

            if (result is not null)
            {
                foreach (var item in result)
                {
                    Items.Add(item);
                }
            }

            StatusMessage =
                Items.Count == 1
                    ? "1 record"
                    : $"{Items.Count:N0} records";
        }
        catch (Exception ex)
        {
            Items.Clear();
            SelectedItem = null;

            StatusMessage =
                $"Unable to load records: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ============================================================
    // MODULE DATA SOURCE
    // ============================================================

    protected abstract Task<IEnumerable<T>> LoadItemsAsync(
        ListSearchOption search);


    // ============================================================
    // DOCUMENT PRINT
    // ============================================================

    private bool CanPrintSelected()
    {
        return
            !IsBusy &&
            SupportsDocumentPrint &&
            SelectedItem is not null;
    }


    [RelayCommand(
        CanExecute = nameof(CanPrintSelected))]
    private void PrintSelected()
    {
        if (SelectedItem is null)
            return;


        PrintItem(
            SelectedItem);
    }


    protected virtual void PrintItem(
        T item)
    {
    }


    // ============================================================
    // PROPERTY CHANGES
    // ============================================================

    partial void OnSelectedItemChanged(
        T? value)
    {
        PrintSelectedCommand
            .NotifyCanExecuteChanged();
    }


    partial void OnIsBusyChanged(
        bool value)
    {
        RefreshCommand
            .NotifyCanExecuteChanged();

        PrintSelectedCommand
            .NotifyCanExecuteChanged();
    }


    // ============================================================
    // HELPER
    // ============================================================

    protected void ConfigureFilter(
        ListFilterDefinition definition)
    {
        FilterDefinition =
            definition;


        OnPropertyChanged(
            nameof(FilterDefinition));

        OnPropertyChanged(
            nameof(HasAdditionalFilter));

        OnPropertyChanged(
            nameof(IsTextFilter));

        OnPropertyChanged(
            nameof(IsSelectionFilter));
    }
}
