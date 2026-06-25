using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierMetalTransactionController : ControllerBase
    {

        private readonly IRepositoryBase<SupplierMetalTransaction> _supplierMetalTransaction;
        private readonly ILogger<SupplierMetalTransactionController> _logger;

        public SupplierMetalTransactionController(IRepositoryBase<SupplierMetalTransaction> supplierMetalTransRepo,
                                            ILogger<SupplierMetalTransactionController> logger)
        {
            _supplierMetalTransaction = supplierMetalTransRepo;
            _logger = logger;
        }

        // GET: api/<SupplierMetalTransactionController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Supplier Transaction");
            return Ok(_supplierMetalTransaction.GetAll());
        }

        // GET api/<SupplierMetalTransactionController>/5
        [HttpGet("{supplierGkey}")]
        public async Task<IActionResult> GetBySupplierGkey(Decimal supplierGkey)  //this will return list - needs to change
        {
            return Ok(_supplierMetalTransaction.Get(x => x.SupplierGkey == supplierGkey));
        }

        // GET api/<SupplierMetalTransactionController>/5
        [HttpGet("{tranType}")]
        public async Task<IActionResult> GetTransactionType(Decimal transactionType)  //this will return list - needs to change
        {
            return Ok(_supplierMetalTransaction.Get(x => x.TransactionType == transactionType));
        }

        // GET api/<SupplierMetalTransactionController>/5
        [HttpGet("{refNbr}")]
        public async Task<IActionResult> GetLastByProductSku(string refNbr)
        {

            var productTrans = _supplierMetalTransaction.GetList(x => x != null && x.ReferenceNbr == refNbr)
                                                               .OrderByDescending(x => x.Gkey)
                                                               .FirstOrDefault();

            return Ok(productTrans);
        }

/*        // GET api/<SupplierMetalTransactionController>/5
        [HttpGet("lastTransaction/Category/{productCategory}")]
        public async Task<IActionResult> GetLastByProductCategory(string productCategory)
        {
            // if null appplication crashes, hence added null check
            var productTrans = _supplierMetalTransaction.GetList(x => x != null && x.ProductCategory == productCategory)
                                                                 .OrderByDescending(x => x.Gkey)
                                                                 .FirstOrDefault();

            return Ok(productTrans);

        }*/

/*        // POST api/<SupplierMetalTransactionController>
        [HttpPost]
        public IActionResult Post([FromBody] ProductTransaction value)
        {
            _supplierMetalTransaction.Add(value);

            return Ok(value);

        }*/


        // PUT api/<SupplierMetalTransactionController>/5
        [HttpPut("{transGkey}")]
        public IActionResult Put(int transactionGkey, [FromBody] SupplierMetalTransaction value)
        {
            _supplierMetalTransaction.Update(value);

            return Ok(value);

        }

/*        // DELETE api/<SupplierMetalTransactionController>/5
        [HttpDelete("{transGkey}")]
        public async Task<IActionResult> Delete(decimal transGkey)
        {
            var productTrans = _productTransaction.Get(x => x.Gkey == transGkey);
            if (productTrans is not null)
                _productTransaction.Remove(productTrans);

            return Ok();
        }*/
    }

}