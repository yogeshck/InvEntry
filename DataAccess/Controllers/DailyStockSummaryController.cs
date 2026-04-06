using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DailyStockSummaryController : ControllerBase
    {
        private readonly IRepositoryBase<DailyStockSummary> _dailyStockSummary;
        private readonly ILogger<ProductStockSummaryController> _logger;

        public DailyStockSummaryController(IRepositoryBase<DailyStockSummary> dailyStockSummaryRepo,
                                            ILogger<ProductStockSummaryController> logger)
        {
            _dailyStockSummary = dailyStockSummaryRepo;
            _logger = logger;
        }

        // GET: api/<RepDailyStockSummary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Daily Stock Summary");
            return Ok(_dailyStockSummary.GetAll());
        }

        // GET: api/<DailyStockSummary>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<DailyStockSummary> FilterHeader([FromBody] DateSearchOption criteria)
        {
            return _dailyStockSummary.GetList(x => x.TransactionDate.HasValue && 
                                                      x.TransactionDate.Value.Date >= criteria.From.Date &&
                                                      x.TransactionDate.Value.Date <= criteria.To.Date)
                                                        .OrderBy(x => x.TransactionDate)
                                                        .OrderBy(x => x.Metal);
        }

        // POST api/<DailyStockSummary>
        [HttpPost]
        public IActionResult Post([FromBody] DailyStockSummary value)
        {
            _dailyStockSummary.Add(value);
            return Ok(value);

        }

        // PUT api/<DailyStockSummary>/5
        [HttpPut("{productGkey}")]
        public IActionResult Put(int productGkey, [FromBody] DailyStockSummary value)
        {
            _dailyStockSummary.Update(value);
            return Ok(value);
        }

    }
}
