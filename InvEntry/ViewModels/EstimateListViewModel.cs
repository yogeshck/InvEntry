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

public class EstimateListViewModel
    : BaseListViewModel<EstimateHeader>
{
    private readonly IEstimateService _estimateService;

    private readonly IDialogService
        _reportDialogService;


    public EstimateListViewModel(
        IEstimateService estimateService,

        [FromKeyedServices("ReportDialogService")]
        IDialogService reportDialogService)

        : base(
            EstimateListDefinition.Create())
    {
        _estimateService =
            estimateService;

        _reportDialogService =
            reportDialogService;
    }


    protected override async Task<IEnumerable<EstimateHeader>>
        LoadItemsAsync(
            ListSearchOption search)
    {
        var option =
            new DateSearchOption
            {
                From = search.From,
                To = search.To
            };


        var result =
            await _estimateService.GetAll(
                option);


        return result ??
               Enumerable.Empty<EstimateHeader>();
    }


    protected override void PrintItem(
        EstimateHeader item)
    {
        if (string.IsNullOrWhiteSpace(
                item.EstNbr))
        {
            return;
        }


        _reportDialogService
            .PrintPreviewEstimate(
                item.EstNbr,
                item.GKey);
    }
}