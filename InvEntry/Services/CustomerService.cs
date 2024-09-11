using DevExpress.Xpf.Data.Native;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface ICustomerService
{
    Task<Customer> GetCustomer(string productId);

    Task CreatCustomer(Customer product);

    Task UpdateCustomer(Customer product);
}

public class CustomerService : ICustomerService
{
    private readonly IMijmsApiService _mijmsApiService;

    public CustomerService(IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public async Task CreatCustomer(Customer customer)
    {
        await _mijmsApiService.Post($"api/customer/", customer);
    }

    public async Task<Customer> GetCustomer(string mobileNbr)
    {
        return await _mijmsApiService.Get<Customer>($"api/customer/{mobileNbr}");
    }

    public async Task UpdateCustomer(Customer customer)
    {
        await _mijmsApiService.Put($"api/customer/{customer.MobileNbr}", customer);
    }
}
