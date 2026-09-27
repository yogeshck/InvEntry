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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductStockSummaryController> _logger;

        public ProductStockSummaryController(IRepositoryBase<ProductStockSummary> _productStockSummaryRepo,
                                            IUnitOfWork unitOfWork,
                                            ILogger<ProductStockSummaryController> logger)
        {
            _productStockSummary = _productStockSummaryRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: api/<ProductStockSummaryController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Stock Summary");
            return Ok(_productStockSummary.GetAll().OrderBy(x => x.ProductSku));   //using productsku instead of metal
        }

        [HttpGet("gkey/{stockSummaryGkey:int}")]
        public IActionResult GetByGkey(int stockSummaryGkey)
        {
            if (stockSummaryGkey <= 0)
                return BadRequest("Product stock summary GKey must be greater than zero.");

            var summary = _productStockSummary.Get(x => x.Gkey == stockSummaryGkey);
            return summary is null
                ? NotFound($"PRODUCT_STOCK_SUMMARY GKey {stockSummaryGkey} was not found.")
                : Ok(summary);
        }

        // GET api/<ProductStockSummaryController>/5
        [HttpGet("productGkey/{productGkey}")]
        public async Task<IActionResult> GetByProductGkey(int productGkey)
        {
            return Ok(_productStockSummary.Get(x => x.ProductGkey == productGkey));
        }

        // GET api/<ProductStockSummaryController>/5
        [HttpGet("productSku/{productSku}")]
        public async Task<IActionResult> GetByProductSku(string productSku)
        {
            return Ok(_productStockSummary.Get(x => x.ProductSku == productSku));
        }

        // GET api/<ProductStockSummaryController>/5
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            return Ok(_productStockSummary.Get(x => x.Category == category));
        }


        // POST api/<ProductStockSummaryController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProductStockSummary value)
        {
            _productStockSummary.Add(value);
            await _unitOfWork.SaveChangesAsync();

            return Ok(value);
        }

        // PUT api/<ProductStockSummaryController>/5
        [HttpPut("{productGkey}")]
        public async Task<IActionResult> Put(int productGkey, [FromBody] ProductStockSummary value)
        {
            _productStockSummary.Update(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

        // DELETE api/<ProductStockSummaryController>/5
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
