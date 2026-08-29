using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Models.UI;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public class InvoiceListViewModel
    : BaseListViewModel<InvoiceHeader>
{
    private readonly IInvoiceService _invoiceService;

    private readonly IDialogService
        _reportDialogService;


    public InvoiceListViewModel(
        IInvoiceService invoiceService,

        [FromKeyedServices("ReportDialogService")]
        IDialogService reportDialogService)

        : base(
            InvoiceListDefinition.Create())
    {
        _invoiceService =
            invoiceService;

        _reportDialogService =
            reportDialogService;
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
            await _invoiceService.GetAll(
                option);


        return result ??
               Enumerable.Empty<InvoiceHeader>();
    }


    protected override void PrintItem(
        InvoiceHeader item)
    {
        if (string.IsNullOrWhiteSpace(
                item.InvNbr))
        {
            return;
        }


        _reportDialogService
            .PrintPreview(
                item.InvNbr);
    }
}