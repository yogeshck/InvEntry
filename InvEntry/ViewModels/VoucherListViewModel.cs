using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using InvEntry.Extension;
using InvEntry.Models;
using InvEntry.Models.UI;
using InvEntry.Services;
using InvEntry.Utils.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public class VoucherListViewModel
    : BaseListViewModel<VoucherDbView>
{
    private readonly IVoucherDbViewService
        _voucherDbViewService;

    private readonly IDialogService
        _reportDialogService;

    private readonly IAdvanceReceiptPrinter
        _receiptPrinter;


    public VoucherListViewModel(
        IVoucherDbViewService voucherDbViewService,

        [FromKeyedServices("ReportDialogService")]
        IDialogService reportDialogService,

        IAdvanceReceiptPrinter receiptPrinter)

        : base(VoucherListDefinition.Create())
    {
        _voucherDbViewService =
            voucherDbViewService;

        _reportDialogService =
            reportDialogService;

        _receiptPrinter =
            receiptPrinter;
    }


    protected override async Task<IEnumerable<VoucherDbView>>
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
                        : search.FilterValue
            };


        var result =
            await _voucherDbViewService
                .GetAll(option);


        return result ?? [];
    }


    protected override void PrintItem(
        VoucherDbView item)
    {
        // Advance and RD installment receipts reprint on their own layout,
        // with balances and installments as they stood at the time.
        if (_receiptPrinter.Handles(
                item.TransType,
                item.VoucherType))
        {
            _ = _receiptPrinter.PreviewAsync(
                item.GKey,
                isReprint: true);

            return;
        }

        _reportDialogService
            .PrintPreviewVoucher(item.GKey);
    }
}