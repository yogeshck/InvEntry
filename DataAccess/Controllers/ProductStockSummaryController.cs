using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductStockSummaryController : ControllerBase
    {
        private readonly IRepositoryBase<ProductStockSummary> _productStockSummary;
        private readonly ILogger<ProductStockSummaryController> _logger;

        public ProductStockSummaryController(
                                                IRepositoryBase<ProductStockSummary> _productStockSumryRepo, 
                                                ILogger<ProductStockSummaryController> logger)
        {
            _productStockSummary = _productStockSumryRepo;
            _logger = logger;
        }

        // GET: api/<ProductStockSummaryController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Stock Summary");
            return Ok(_productStockSummary.GetAll());
        }

        // GET api/<ProductStockSummaryController>/5
        [HttpGet("{productGkey}")]
        public async Task<IActionResult> Get(int productGkey)
        {
            return Ok(_productStockSummary.Get(x => x.ProductGkey == productGkey));
        }

        // POST api/<ProductStockSummaryController>
        [HttpPost]
        public void Post([FromBody] ProductStockSummary value)
        {
            _productStockSummary.Add(value);
        }

        // PUT api/<ProductStockSummaryController>/5
        [HttpPut("{productGkey}")]
        public void Put(decimal productGkey, [FromBody] ProductStockSummary value)
        {
            _productStockSummary.Update(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{productGkey}")]
        public async Task<IActionResult> Delete(decimal productGkey)
        {
            var product = _productStockSummary.Get(x => x.ProductGkey == productGkey);

            if (product is not null)
                _productStockSummary.Remove(product);

            return Ok();
        }
    }
}
