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


/*        // POST api/<AddressController>
        [HttpPost("address")]
        public IActionResult Post([FromBody] OrgAddress value)
        {
            _repository.Add(value);
            return Ok(value);
        }*/

        // PUT api/<AddressController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] OrgAddress value)
        {
            value.Gkey = id;
            _repository.Update(value);
        }


    }
}
