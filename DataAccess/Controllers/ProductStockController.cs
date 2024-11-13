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
        private readonly IRepositoryBase<ProductStock> _productStock;
        private readonly ILogger<ProductStockController> _logger;

        public ProductStockController( IRepositoryBase<ProductStock> _productStockRepo, 
                                        ILogger<ProductStockController> logger) 
        {
            _productStock = _productStockRepo;
            _logger = logger;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Stock");
            return Ok(_productStock.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{productSku}")]
        public async Task<IActionResult> Get(string productSku)
        {
            return Ok(_productStock.Get(x => x.ProductSku == productSku));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public void Post([FromBody] ProductStock value)
        {
             _productStock.Add(value);
        }

        // PUT api/<ProductStockController>/5
        [HttpPut("{productGkey}")]
        public void Put(decimal productGkey, [FromBody] ProductStock value)
        {
            _productStock.Update(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{productGkey}")]
        public async Task<IActionResult> Delete(decimal productGkey)
        {
            var product = _productStock.Get(x => x.ProductGkey == productGkey);

            if(product is not null)
                _productStock.Remove(product);

            return Ok();
        }
    }
}
