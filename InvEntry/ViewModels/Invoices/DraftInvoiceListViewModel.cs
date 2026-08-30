using DevExpress.Mvvm;
using InvEntry.Models;
using InvEntry.Models.UI;
using InvEntry.Services;
using InvEntry.Store;
using InvEntry.Utils.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public class DraftInvoiceListViewModel
    : BaseListViewModel<InvoiceHeader>
{
    private readonly IInvoiceService _invoiceService;
    //private readonly INavigationService _navigationService;
    private readonly InvoiceEditSession _invoiceEditSession;

    public DraftInvoiceListViewModel(
        IInvoiceService invoiceService,
        InvoiceEditSession invoiceEditSession)
        : base(DraftInvoiceListDefinition.Create())
    {
        _invoiceService = invoiceService;
        //_navigationService = navigationService;
        _invoiceEditSession = invoiceEditSession;
    }


    protected override async Task<IEnumerable<InvoiceHeader>>
        LoadItemsAsync(
            ListSearchOption search)
    {
        var option =
            new DateSearchOption
            {
                From = search.From,
                To = search.To,

                Filter1 =
                    string.IsNullOrWhiteSpace(
                        search.FilterValue)
                        ? null
                        : search.FilterValue.Trim()
            };

        var result =
            await _invoiceService
                .GetDraftsAsync(option);

        return result ??
               Enumerable.Empty<InvoiceHeader>();
    }


    protected override async Task OpenItemAsync(
        InvoiceHeader item)
    {
        if (item is null || item.GKey <= 0)
            return;

        try
        {
            IsBusy = true;
            StatusMessage = "Loading draft invoice...";

            var draft =
                await _invoiceService
                    .GetForEditAsync(item.GKey);

            if (draft is null)
                return;

            _invoiceEditSession.SetDraft(draft);

            Messenger.Default.Send(
                "InvoiceEntryPage",
                "NavigateToPage");
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Unable to open draft: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

}