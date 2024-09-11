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
       private readonly IRepositoryBase<OrgCustomer> _product;

        public CustomerController(IRepositoryBase<OrgCustomer> productRepo)
        {
            _product = productRepo;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(_product.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{mobile}")]
        public async Task<IActionResult> Get(string mobile)
        {
            return Ok(_product.Get(x => x.MobileNbr == mobile));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public void Post([FromBody] OrgCustomer value)
        {
            _product.Add(value);
        }

        // PUT api/<ProductStockController>/5
        [HttpPut("{MobileNbr}")]
        public void Put(string MobileNbr, [FromBody] OrgCustomer value)
        {
            _product.Update(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{MobileNbr}")]
        public async Task<IActionResult> Delete(string MobileNbr)
        {
            var product = _product.Get(x => x.MobileNbr == MobileNbr);

            if (product is not null)
                _product.Remove(product);

            return Ok();
        }
    }
}
