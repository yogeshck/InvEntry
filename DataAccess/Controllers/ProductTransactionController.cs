using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductTransactionController : ControllerBase
    {
            private readonly IRepositoryBase<ProductTransaction> _productTransaction;
            private readonly ILogger<ProductTransactionController> _logger;

            public ProductTransactionController(IRepositoryBase<ProductTransaction> _productTransRepo,
                                            ILogger<ProductTransactionController> logger)
            {
                _productTransaction = _productTransRepo;
                _logger = logger;
            }

        // GET: api/<ProductTransactionController>
        [HttpGet]
            public async Task<IActionResult> GetAll()
            {
                _logger.LogInformation("All Product Transaction");
                return Ok(_productTransaction.GetAll());
            }

            // GET api/<ProductTransactionController>/5
            [HttpGet("{productSku}")]
            public async Task<IActionResult> GetByProductSku(string productSku)
            {
                return Ok(_productTransaction.Get(x => x.ProductSku == productSku));
            }

            // POST api/<ProductTransactionController>
            [HttpPost]
            public void Post([FromBody] ProductTransaction value)
            {
                _productTransaction.Add(value);
            }

            // PUT api/<ProductTransactionController>/5
            [HttpPut("{transGkey}")]
            public void Put(int transactionGkey, [FromBody] ProductTransaction value)
            {
                _productTransaction.Update(value);
            }

            // DELETE api/<ProductTransactionController>/5
            [HttpDelete("{transGkey}")]
            public async Task<IActionResult> Delete(decimal transGkey)
            {
                var productTrans = _productTransaction.Get(x => x.Gkey == transGkey);

                if (productTrans is not null)
                    _productTransaction.Remove(productTrans);

                return Ok();
            }
        }
    
}
