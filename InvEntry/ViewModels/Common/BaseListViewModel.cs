using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Models.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public abstract partial class BaseListViewModel<T>
    : ObservableObject
{
    // ============================================================
    // FIELDS
    // ============================================================

    private readonly ListViewDefinition _definition;


    // ============================================================
    // OBSERVABLE PROPERTIES
    // ============================================================

    [ObservableProperty]
    private ObservableCollection<T> items = new();


    [ObservableProperty]
    private T? selectedItem;


    [ObservableProperty]
    private ListSearchOption searchOption = new();


    [ObservableProperty]
    private bool isBusy;


    [ObservableProperty]
    private string statusMessage = string.Empty;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    protected BaseListViewModel(
        ListViewDefinition definition)
    {
        _definition =
            definition ??
            throw new ArgumentNullException(
                nameof(definition));

        InitializeSearchOptions();

    }

    private void InitializeSearchOptions()
    {
        SearchOption.From =
            DateTime.Today.AddDays(
                _definition.DefaultFromDays);

        SearchOption.To =
            DateTime.Today.AddDays(
                _definition.DefaultToDays);

        SearchOption.FilterValue =
            _definition.Filter.DefaultValue;
    }


    // ============================================================
    // UI DEFINITION
    // ============================================================

    public ListViewDefinition Definition =>
        _definition;


    public string Title =>
        _definition.Title;


    public string Description =>
        _definition.Description;


    public IReadOnlyList<ListColumnDefinition> Columns =>
        _definition.Columns;


    public ListFilterDefinition FilterDefinition =>
        _definition.Filter;


    public IReadOnlyList<RowFormatDefinition> RowFormats =>
        _definition.RowFormats;


    public bool SupportsDocumentPrint =>
        _definition.SupportsDocumentPrint;

    public bool SupportsOpen =>
    _definition.SupportsOpen;


    public string OpenButtonText =>
        _definition.OpenButtonText;

    // ============================================================
    // FILTER HELPERS
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


    public DateTime Today =>
        DateTime.Today;


    // ============================================================
    // DATA RETRIEVAL
    // ============================================================

    protected abstract Task<IEnumerable<T>>
        LoadItemsAsync(
            ListSearchOption search);


    // ============================================================
    // REFRESH
    // ============================================================

    private bool CanRefresh()
    {
        return !IsBusy;
    }


    [RelayCommand(
        CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        if (IsBusy)
            return;


        try
        {
            IsBusy = true;

            StatusMessage =
                "Loading records...";

            SelectedItem = default;


            var result =
                await LoadItemsAsync(
                    SearchOption);


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

            SelectedItem = default;

            StatusMessage =
                $"Unable to load records: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ============================================================
    // OPEN / EDIT
    // ============================================================

    private bool CanOpenSelected()
    {
        return
            !IsBusy &&
            SupportsOpen &&
            SelectedItem is not null;
    }


    [RelayCommand(
        CanExecute = nameof(CanOpenSelected))]
    private async Task OpenSelectedAsync()
    {
        if (SelectedItem is null)
            return;

        await OpenItemAsync(
            SelectedItem);
    }


    protected virtual Task OpenItemAsync(
        T item)
    {
        return Task.CompletedTask;
    }


    // ============================================================
    // PRINT
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
        OpenSelectedCommand
            .NotifyCanExecuteChanged();

        PrintSelectedCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(
        bool value)
    {
        RefreshCommand
            .NotifyCanExecuteChanged();

        OpenSelectedCommand
            .NotifyCanExecuteChanged();

        PrintSelectedCommand
            .NotifyCanExecuteChanged();
    }

}