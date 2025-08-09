using DevExpress.Xpf.Data.Native;
using DevExpress.XtraDataLayout;
using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface ICustomerService
{
    Task<Customer> GetCustomer(string mobileNbr);

    Task<Customer> CreateCustomer(Customer customer);

    Task UpdateCustomer(Customer customer);
}

public class CustomerService : ICustomerService
{
    private readonly IMijmsApiService _mijmsApiService;

    public CustomerService(IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public async Task<Customer> CreateCustomer(Customer customer)
    {
        var orgAddress = await _mijmsApiService.Post($"api/address/",customer.Address);
        customer.AddressGkey = orgAddress.GKey;
        customer.GstStateCode = orgAddress.GstStateCode;
        return await _mijmsApiService.Post($"api/customer/", customer);
    }

    public async Task<Customer> GetCustomer(string mobileNbr)
    {

        var customer = await _mijmsApiService.Get<Customer>($"api/customer/{mobileNbr}");
        if (customer is null)
        {
            return customer;
        }

        var orgAddress = await _mijmsApiService.Get<OrgAddress>($"api/address/{customer.AddressGkey}");
        if (orgAddress is not null)
        {
            customer.Address = orgAddress;
        }

        return customer;
    }

    public async Task UpdateCustomer(Customer customer)
    {

        var savedAddress = await _mijmsApiService.Post($"api/address/", customer.Address);

        if (savedAddress is not null)
        {
            customer.AddressGkey = savedAddress.GKey;
        }
            await _mijmsApiService.Put($"api/customer/{customer.MobileNbr}", customer);
     
    }
}
