using DevExpress.Mvvm;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Models.UI;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public class OldMetalTransactionListViewModel
    : BaseListViewModel<OldMetalTransaction>
{
    private readonly IOldMetalTransactionService
        _oldMetalTransService;

    private readonly IDialogService
        _reportDialogService;


    public OldMetalTransactionListViewModel(
        IOldMetalTransactionService oldMetalTransService,
        IDialogService reportDialogService)

        : base(OldMetalTransactionListDefinition.Create())
    {
        _oldMetalTransService =
            oldMetalTransService;

        _reportDialogService =
            reportDialogService;
    }


    protected override async Task<IEnumerable<OldMetalTransaction>>
        LoadItemsAsync(ListSearchOption search)
    {
        var option = new DateSearchOption
        {
            From = search.From,
            To = search.To,

            Filter1 =
                string.IsNullOrWhiteSpace(search.FilterValue)
                    ? null
                    : search.FilterValue.Trim()
        };


        var result =
            await _oldMetalTransService.GetAll(option);


        return result ?? [];
    }


    protected override void PrintItem(
        OldMetalTransaction item)
    {
        if (item.TransType == "OG Purchase")
        {
            if (string.IsNullOrWhiteSpace(item.TransNbr))
                return;

            _reportDialogService
                .PrintPreviewOMPurchase(item.TransNbr);

            return;
        }


        if (string.IsNullOrWhiteSpace(item.DocRefNbr) ||
            item.DocRefGkey is null)
        {
            return;
        }


        _reportDialogService
            .PrintPreviewDeliveryNote(
                item.DocRefNbr,
                item.DocRefGkey.Value,
                null);
    }
}