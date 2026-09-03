using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvEntry.Contracts.Invoices;
using InvEntry.Models;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels.Invoices;

public partial class DraftInvoicePickerViewModel : ObservableObject
{
    private readonly IInvoiceService _invoiceService;

    private readonly ObservableCollection<InvoiceHeader> _allDrafts = new();


    // =========================================================
    // DISPLAYED DRAFTS
    // =========================================================

    public ObservableCollection<InvoiceHeader> Drafts { get; }
        = new();


    // =========================================================
    // SELECTED DRAFT
    // =========================================================

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenSelectedCommand))]
    private InvoiceHeader? selectedDraft;


    // =========================================================
    // SEARCH
    // =========================================================

    [ObservableProperty]
    private string? searchText;


    // =========================================================
    // BUSY / MESSAGE
    // =========================================================

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;


    // =========================================================
    // RESULT
    // =========================================================

    public InvoiceEditResponse? SelectedInvoice { get; private set; }


    // =========================================================
    // CLOSE REQUEST
    // =========================================================

    public event Action<bool?>? RequestClose;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public DraftInvoicePickerViewModel(
        IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }


    // =========================================================
    // SEARCH CHANGED
    // =========================================================

    partial void OnSearchTextChanged(string? value)
    {
        ApplyFilter();
    }


    // =========================================================
    // LOAD DRAFTS
    // =========================================================

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading draft invoices...";

            _allDrafts.Clear();
            Drafts.Clear();

            var options = new DateSearchOption();

            var result =
                await _invoiceService
                    .GetDraftsAsync(options);

            foreach (var draft in
                     result
                         .OrderByDescending(x => x.InvDate)
                         .ThenByDescending(x => x.GKey))
            {
                _allDrafts.Add(draft);
            }

            ApplyFilter();

            StatusMessage =
                Drafts.Count == 0
                    ? "No draft invoices found."
                    : $"{Drafts.Count} draft invoice(s)";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Unable to load draft invoices. {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }


    // =========================================================
    // FILTER
    // =========================================================

    private void ApplyFilter()
    {
        Drafts.Clear();

        var search =
            SearchText?.Trim();

        var query =
            _allDrafts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query =
                query.Where(x =>
                    Contains(
                        $"DRAFT-{x.GKey}",
                        search)
                    ||
                    Contains(
                        x.CustMobile,
                        search)
                //    ||
                //    Contains(
                //        x.CustomerName,
                //        search)
                    ||
                    Contains(
                        x.InvNbr,
                        search));
        }

        foreach (var draft in query)
        {
            Drafts.Add(draft);
        }

        StatusMessage =
            Drafts.Count == 0
                ? "No matching draft invoices."
                : $"{Drafts.Count} draft invoice(s)";
    }


    private static bool Contains(
        string? value,
        string search)
    {
        return !string.IsNullOrWhiteSpace(value)
               &&
               value.Contains(
                   search,
                   StringComparison.OrdinalIgnoreCase);
    }


    // =========================================================
    // OPEN SELECTED
    // =========================================================

    private bool CanOpenSelected()
    {
        return SelectedDraft is not null
               && !IsBusy;
    }


    [RelayCommand(CanExecute = nameof(CanOpenSelected))]
    private async Task OpenSelected()
    {
        if (SelectedDraft is null)
            return;

        try
        {
            IsBusy = true;

            StatusMessage =
                $"Opening DRAFT-{SelectedDraft.GKey}...";

            SelectedInvoice =
                await _invoiceService
                    .GetForEditAsync(
                        SelectedDraft.GKey);

            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Unable to open draft invoice. {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }


    // =========================================================
    // CANCEL
    // =========================================================

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
