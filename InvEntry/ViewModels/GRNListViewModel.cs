using InvEntry.Models;
using InvEntry.Models.UI;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public class GRNListViewModel
    : BaseListViewModel<GrnDbView>
{
    private readonly IGrnDbViewService _grnDbViewService;


    public GRNListViewModel(
        IGrnDbViewService grnDbViewService)
        : base(GRNListDefinition.Create())
    {
        _grnDbViewService = grnDbViewService;
    }


    protected override async Task<IEnumerable<GrnDbView>>
        LoadItemsAsync(ListSearchOption search)
    {
        var option = new DateSearchOption
        {
            From = search.From,
            To = search.To
        };

        var result =
            await _grnDbViewService.GetAll(option);

        return result ?? [];
    }
}