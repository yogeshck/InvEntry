using InvEntry.Contracts.CustomerOrders;
using InvEntry.Models;
using InvEntry.Utils.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface ICustomerOrderService
{
    Task<CustomerOrder> GetCustomerOrder(string orderNbr);

    Task<IEnumerable<CustomerOrder>> GetAll(
        DateSearchOption options);

    Task<IEnumerable<CustomerOrderLine>> GetLines(
        string orderNbr);

    Task<SaveCustomerOrderResponse> SaveAsync(
        SaveCustomerOrderRequest request);

    Task UpdateHeader(CustomerOrder customerOrder);
}

public class CustomerOrderService : ICustomerOrderService
{
    private readonly IMijmsApiService _mijmsApiService;

    public CustomerOrderService(
        IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public async Task<CustomerOrder> GetCustomerOrder(
        string orderNbr)
    {
        return await _mijmsApiService.Get<CustomerOrder>(
            $"api/customerOrder/{orderNbr}");
    }

    public async Task<IEnumerable<CustomerOrder>> GetAll(
        DateSearchOption options)
    {
        return await _mijmsApiService
            .PostEnumerable<CustomerOrder, DateSearchOption>(
                "api/customerOrder/filter",
                options);
    }

    public async Task<IEnumerable<CustomerOrderLine>> GetLines(
        string orderNbr)
    {
        return await _mijmsApiService
            .GetEnumerable<CustomerOrderLine>(
                $"api/customerOrderLine/{orderNbr}");
    }

    public async Task<SaveCustomerOrderResponse> SaveAsync(
    SaveCustomerOrderRequest request)
    {
        return await _mijmsApiService
            .Post<SaveCustomerOrderRequest, SaveCustomerOrderResponse>(
                "api/customerOrder/save",
                request);
    }

    public async Task UpdateHeader(
        CustomerOrder customerOrder)
    {
        await _mijmsApiService.Put(
            $"api/customerOrder/{customerOrder.OrderNbr}",
            customerOrder);
    }
}