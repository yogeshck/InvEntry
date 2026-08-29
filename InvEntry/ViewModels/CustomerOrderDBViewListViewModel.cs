using InvEntry.Models;
using InvEntry.Models.UI;
using InvEntry.Services;
using InvEntry.Utils.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.ViewModels;

public class CustomerOrderDBViewListViewModel
    : BaseListViewModel<CustomerOrderDBView>
{
    private readonly ICustomerOrderDbViewService
        _customerOrderDbViewService;


    public CustomerOrderDBViewListViewModel(
        ICustomerOrderDbViewService customerOrderDbViewService)
        : base(CustomerOrderListDefinition.Create())
    {
        _customerOrderDbViewService =
            customerOrderDbViewService;
    }


    protected override async Task<IEnumerable<CustomerOrderDBView>>
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
            await _customerOrderDbViewService
                .GetAll(option);


        return result ?? [];
    }
}