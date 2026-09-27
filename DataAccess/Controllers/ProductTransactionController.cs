using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductTransactionController : ControllerBase
    {
        private readonly IRepositoryBase<ProductTransaction> _productTransaction;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductTransactionController> _logger;

        public ProductTransactionController(IRepositoryBase<ProductTransaction> productTransRepo,
                                            IUnitOfWork unitOfWork,
                                            ILogger<ProductTransactionController> logger)
        {
            _productTransaction = productTransRepo;
            _unitOfWork = unitOfWork;
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
        public async Task<IActionResult> GetByProductSku(string productSku)  //this will return list - needs to change
        {
            var transaction = _productTransaction.Get(x => x.ProductSku == productSku);
            return transaction is null ? NotFound() : Ok(transaction);
        }

        // GET api/<ProductTransactionController>/5
        [HttpGet("category/{productCategory}")]
        public async Task<IActionResult> GetByProductCategory(string productCategory)  //this will return list - needs to change
        {
            var transaction = _productTransaction
                .GetList(x => x.ProductCategory == productCategory)
                .OrderByDescending(x => x.Gkey)
                .FirstOrDefault();

            return transaction is null ? NotFound() : Ok(transaction);
        }

        // GET api/<ProductTransactionController>/5
        [HttpGet("lastTransaction/{productSku}")]
        public async Task<IActionResult> GetLastByProductSku(string productSku)
        {

            var productTrans = _productTransaction.GetList(x => x != null && x.ProductSku == productSku)
                                                               .OrderByDescending(x => x.Gkey)
                                                               .FirstOrDefault();

            return Ok(productTrans);                                   
        }

        // GET api/<ProductTransactionController>/5
        [HttpGet("lastTransaction/Category/{productCategory}")]
        public async Task<IActionResult> GetLastByProductCategory(string productCategory)
        {
          // if null appplication crashes, hence added null check
           var productTrans = _productTransaction.GetList(x => x != null && x.ProductCategory == productCategory )
                                                                .OrderByDescending(x => x.Gkey)
                                                                .FirstOrDefault();

           return productTrans is null ? NotFound() : Ok(productTrans);
        
        }

        // POST api/<ProductTransactionController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProductTransaction value)
        {
            bool isSourceIdentifiedGrnReceipt =
                string.Equals(value.DocumentType, "GRN", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(value.TransactionType, "Receipt", StringComparison.OrdinalIgnoreCase) &&
                value.SourceLineGkey.HasValue;

            ProductTransaction? FindExisting()
            {
                if (isSourceIdentifiedGrnReceipt)
                {
                    return _productTransaction.Get(x =>
                        x.DocumentType == "GRN" &&
                        x.TransactionType == "Receipt" &&
                        x.SourceLineGkey == value.SourceLineGkey);
                }

                if (string.IsNullOrWhiteSpace(value.DocumentNbr) ||
                    string.IsNullOrWhiteSpace(value.DocumentType) ||
                    !value.RefGkey.HasValue)
                {
                    return null;
                }

                return _productTransaction.Get(x =>
                    x.DocumentNbr == value.DocumentNbr &&
                    x.DocumentType == value.DocumentType &&
                    x.RefGkey == value.RefGkey &&
                    x.ProductCategory == value.ProductCategory &&
                    x.TransactionType == value.TransactionType);
            }

            var existing = FindExisting();
            if (existing is not null)
            {
                _logger.LogInformation(
                    "Product transaction already exists for {DocumentType} {DocumentNbr}, source line {SourceLineGkey}",
                    value.DocumentType,
                    value.DocumentNbr,
                    value.SourceLineGkey);
                return Ok(existing);
            }

            _productTransaction.Add(value);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
                when (isSourceIdentifiedGrnReceipt)
            {
                _unitOfWork.ClearChanges();
                existing = FindExisting();
                if (existing is not null)
                    return Ok(existing);

                throw;
            }

            return Ok(value);

        }


        // PUT api/<ProductTransactionController>/5
        [HttpPut("{transGkey}")]
        public async Task<IActionResult> Put(int transactionGkey, [FromBody] ProductTransaction value)
        {
           _productTransaction.Update(value);
           await _unitOfWork.SaveChangesAsync();

            return Ok(value);

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
