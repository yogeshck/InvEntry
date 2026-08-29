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


    public VoucherListViewModel(
        IVoucherDbViewService voucherDbViewService,

        [FromKeyedServices("ReportDialogService")]
        IDialogService reportDialogService)

        : base(VoucherListDefinition.Create())
    {
        _voucherDbViewService =
            voucherDbViewService;

        _reportDialogService =
            reportDialogService;
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
        _reportDialogService
            .PrintPreviewVoucher(item.GKey);
    }
}