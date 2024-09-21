using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductStockController : ControllerBase
    {
        private readonly IRepositoryBase<ProductStock> _product;
        private readonly ILogger<ProductStockController> _logger;
        public ProductStockController(IRepositoryBase<ProductStock> productRepo, ILogger<ProductStockController> logger) 
        {
            _product = productRepo;
            _logger = logger;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Stock");
            return Ok(_product.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{productId}")]
        public async Task<IActionResult> Get(string productId)
        {
            return Ok(_product.Get(x => x.ProductId == productId));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public void Post([FromBody] ProductStock value)
        {
             _product.Add(value);
        }

        // PUT api/<ProductStockController>/5
        [HttpPut("{productGkey}")]
        public void Put(decimal productGkey, [FromBody] ProductStock value)
        {
            _product.Update(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{productGkey}")]
        public async Task<IActionResult> Delete(decimal productGkey)
        {
            var product = _product.Get(x => x.ProductGkey == productGkey);

            if(product is not null)
                _product.Remove(product);

            return Ok();
        }
    }
}
