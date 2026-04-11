using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetalsController : ControllerBase
    {
        private readonly IRepositoryBase<Metal> _metalsRepo;

        public MetalsController(IRepositoryBase<Metal> metalsRepo)
        {
            _metalsRepo = metalsRepo;
        }


        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(_metalsRepo.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{metalName}")]
        public async Task<IActionResult> Get(string metalName)
        {
            return Ok(_metalsRepo.Get(x => x.MetalName == metalName));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public IActionResult Post([FromBody] Metal value)
        {
            _metalsRepo.Add(value);

            return Ok(value);
        }


        // PUT api/<ProductStockController>/5
        [HttpPut("{metalGkey}")]
        public IActionResult Put(decimal metalGkey, [FromBody] Metal value)
        {
            _metalsRepo.Update(value);
            return Ok(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{metalGkey}")]
        public async Task<IActionResult> Delete(string metalName)
        {
            var product = _metalsRepo.Get(x => x.MetalName == metalName);

            if (product is not null)
                _metalsRepo.Remove(product);

            return Ok();
        }
    }

}

