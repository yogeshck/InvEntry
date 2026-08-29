using InvEntry.Models;
using InvEntry.Models.UI;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public class DailyStockSummaryListViewModel
    : BaseListViewModel<DailyStockSummary>
{
    private readonly IDailyStockSummaryService
        _dailyStockSummaryService;


    public DailyStockSummaryListViewModel(
        IDailyStockSummaryService dailyStockSummaryService)

        : base(
            DailyStockSummaryListDefinition.Create())
    {
        _dailyStockSummaryService =
            dailyStockSummaryService;
    }


    protected override async Task<IEnumerable<DailyStockSummary>>
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
            await _dailyStockSummaryService
                .GetAll(option);


        return result ?? [];
    }
}