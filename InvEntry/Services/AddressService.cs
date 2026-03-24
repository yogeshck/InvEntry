using InvEntry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IAddressService
{
    Task<OrgAddress> GetAddress(int gkey);

   // Task<List<OrgAddress>> GetAddresses(List<string> mobileNumbers);

    Task<OrgAddress> CreateAddress(OrgAddress orgAddress);

    Task UpdateAddress(OrgAddress orgAddress);
}

public class AddressService : IAddressService
{
    private readonly IMijmsApiService _mijmsApiService;

    public AddressService(IMijmsApiService mijmsApiService)
    {
        _mijmsApiService = mijmsApiService;
    }

    public async Task<OrgAddress> CreateAddress(OrgAddress orgAddress)
    {
           return await _mijmsApiService.Post($"api/address/", orgAddress);
    }

    public async Task<OrgAddress> GetAddress(int gkey)
    {

        var orgAddress = await _mijmsApiService.Get<OrgAddress>($"api/address/{gkey}");
        if (orgAddress is not null)
        {
            return orgAddress; 
        }

        return orgAddress;
    }

    public async Task UpdateAddress(OrgAddress orgAddress)
    {

        await _mijmsApiService.Put($"api/address/{orgAddress.GKey}", orgAddress);

    }
}
