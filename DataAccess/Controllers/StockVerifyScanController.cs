using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class StockVerifyScanController : ControllerBase
    {

        private readonly IRepositoryBase<StockVerifyScan> _verifiedStock;
        private readonly ILogger<StockVerifyScanController> _logger;

        public StockVerifyScanController(IRepositoryBase<StockVerifyScan> _verifiedStockRepo,
                                        ILogger<StockVerifyScanController> logger)
        {
            _verifiedStock = _verifiedStockRepo;
            _logger = logger;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Verified Stock");
            return Ok(_verifiedStock.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{barCode}")]
        public async Task<IActionResult> Get(string barCode)
        {
            return Ok(_verifiedStock.Get(x => x.Barcode == barCode));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public IActionResult Post([FromBody] StockVerifyScan value)
        {
            _verifiedStock.Add(value);
            return Ok(value);

        }

        [HttpPost("bulk")]
        public IActionResult PostList([FromBody] List<StockVerifyScan> scanBuffer)
        {
            if (scanBuffer == null || scanBuffer.Count == 0)
                return BadRequest("No items provided.");

            for (int i = 0; i < scanBuffer.Count; i++)
            {
                var item = scanBuffer[i];
                if (string.IsNullOrEmpty(item.Barcode))
                    return BadRequest($"Item at index {i} has an empty barcode.");

                _verifiedStock.Add(item);
            }
            return Ok(new { Count = scanBuffer.Count, Message = "Bulk insert successful" });
        }


        // PUT api/<ProductStockController>/5
        /*        [HttpPut("{productGkey}")]
                public IActionResult Put(int productGkey, [FromBody] ProductStock value)
                {
                    _verifiedStock.Update(value);
                    return Ok(value);
                }*/

        // DELETE api/<ProductStockController>/5
        /*
        [HttpDelete("{productGkey}")]
        public async Task<IActionResult> Delete(decimal productGkey)
        {
            var product = _verifiedStock.Get(x => x.ProductGkey == productGkey);

            if (product is not null)
                _verifiedStock.Remove(product);

            return Ok();
        }
        */
    }
}
