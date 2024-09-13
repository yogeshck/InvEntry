using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
       private readonly IRepositoryBase<OrgCustomer> _customer;

        public CustomerController(IRepositoryBase<OrgCustomer> productRepo)
        {
            _customer = productRepo;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(_customer.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{mobile}")]
        public async Task<IActionResult> Get(string mobile)
        {
            return Ok(_customer.Get(x => x.MobileNbr == mobile));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public void Post([FromBody] OrgCustomer value)
        {
            _customer.Add(value);
        }

        // PUT api/<ProductStockController>/5
        [HttpPut("{MobileNbr}")]
        public void Put(string MobileNbr, [FromBody] OrgCustomer value)
        {
            _customer.Update(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{MobileNbr}")]
        public async Task<IActionResult> Delete(string MobileNbr)
        {
            var product = _customer.Get(x => x.MobileNbr == MobileNbr);

            if (product is not null)
                _customer.Remove(product);

            return Ok();
        }


    }
}
