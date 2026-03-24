using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : BaseController<OrgAddress>
    {
        
        public AddressController(IRepositoryBase<OrgAddress> orgAddressRepository) : 
                                                base(orgAddressRepository)
        {
        }

        // GET api/<AddressController>/5
        [HttpGet("{id}")]
        public OrgAddress? Get(int id)
        {
            return _repository.Get(x => x.Gkey == id);
        }


        // POST api/<AddressController>
        [HttpPost("address")]
        public IActionResult Post([FromBody] OrgAddress value)
        {
            var address = _repository.Get(x => x.Gkey == value.Gkey);

            if (address == null)
            {
                _repository.Add(value);
                return Ok(value);
            }
            else
            {
                address.AddressLine1 = value.AddressLine1;
                address.AddressLine2 = value.AddressLine2;
                address.City = value.City;
                address.State = value.State;
                address.Country = value.Country;
                address.Pincode = value.Pincode;
                address.GstStateCode = value.GstStateCode;
                _repository.Update(address);
                return Ok(address);
            }
        }

        // PUT api/<AddressController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] OrgAddress value)
        {
            value.Gkey = id;
            _repository.Update(value);
        }


    }
}
