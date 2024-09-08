using InvEntry.UIModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface ICustomerService
{
    public Customer GetCustomer(string phoneNumber);
}

public class CustomerService : ICustomerService
{

    public Customer GetCustomer(string phoneNumber)
    {
        return new Customer() 
        {
            Mobile = phoneNumber,
            Name = $"Customer{Random.Shared.NextInt64(100,999)}",
            Address = $"Address{Random.Shared.NextInt64(100, 999)}",
            Email = $"Email{Random.Shared.NextInt64(100, 999)}@gmail.com"
        };
    }
}
