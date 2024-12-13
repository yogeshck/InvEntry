using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProductTransactionSummaryController : ControllerBase
    {

        private readonly IRepositoryBase<ProductTransactionSummary> _prodTransSumryRepo;
        private readonly ILogger<ProductTransactionController> _logger;

        public ProductTransactionSummaryController(IRepositoryBase<ProductTransactionSummary> prodTransSumryRepo,
                                                    ILogger<ProductTransactionController> logger)

        {
            _prodTransSumryRepo = prodTransSumryRepo;
            _logger = logger;
        }


        // GET: api/<ProductTransactionSummaryController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Transaction Summary");
            return Ok(_prodTransSumryRepo.GetAll());
        }

        // GET api/<ProductTransactionSummaryController>/5
        [HttpGet("lastTransaction/Category/{productCategory}")]
        public async Task<IActionResult> GetLastByProductCategory(string productCategory)
        {

            var productTransSumry = _prodTransSumryRepo.GetList(x => x.ProductCategory == productCategory )
                                                                    .OrderByDescending(x => x.Gkey)
                                                                    .FirstOrDefault();

            return Ok(productTransSumry);

        }

        // GET: api/<ProductTransactionSummaryController>/24-Sep-2024/25-Sep-2024/Category
        [HttpPost("filter")]
        public IEnumerable<ProductTransactionSummary> FilterHeader([FromBody] DateSearchOption criteria)
        {

            if (criteria.Filter1 is not null)
            {
                return _prodTransSumryRepo.GetList(x => x.TransactionDate.HasValue &&
                                                        x.TransactionDate.Value.Date >= criteria.From.Date &&
                                                        x.TransactionDate.Value.Date <= criteria.To.Date &&
                                                        x.ProductCategory == criteria.Filter1)
                                                        .OrderBy(x => x.TransactionDate);
            }
            else
            {
                return _prodTransSumryRepo.GetList(x => x.TransactionDate.HasValue &&
                                                        x.TransactionDate.Value.Date >= criteria.From.Date &&
                                                        x.TransactionDate.Value.Date <= criteria.To.Date)
                                                        .OrderBy(x => x.TransactionDate);

            }
        }

        // POST api/<ProductTransactionSummaryController>
        [HttpPost]
        public void Post([FromBody] ProductTransactionSummary value)
        {
            _prodTransSumryRepo.Add(value);
        }

        // PUT api/<ProductTransactionController>/5
        [HttpPut("{transGkey}")]
        public void Put(int transactionGkey, [FromBody] ProductTransactionSummary value)
        {
            _prodTransSumryRepo.Update(value);
        }

        // DELETE api/<ProductTransactionSummaryController>/5
        [HttpDelete("{transGkey}")]
        public async Task<IActionResult> Delete(decimal transGkey)
        {
            var productTransSumry = _prodTransSumryRepo.Get(x => x.Gkey == transGkey);
            if (productTransSumry is not null)
                _prodTransSumryRepo.Remove(productTransSumry);

            return Ok();
        }
    }
}
