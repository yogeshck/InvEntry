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
        
        public AddressController(IRepositoryBase<OrgAddress> repository) : base(repository)
        {
        }

        // GET api/<AddressController>/5
        [HttpGet("{id}")]
        public OrgAddress? Get(int id)
        {
            return _repository.Get(x => x.Gkey == id);
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
