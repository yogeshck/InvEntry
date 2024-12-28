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
        private readonly IRepositoryBase<RepDailyStockSummary> _repDailyStockSummary;
        private readonly ILogger<ProductStockSummaryController> _logger;

        public DailyStockSummaryController(IRepositoryBase<RepDailyStockSummary> repDailyStockSummaryRepo,
                                            ILogger<ProductStockSummaryController> logger)
        {
            _repDailyStockSummary = repDailyStockSummaryRepo;
            _logger = logger;
        }

        // GET: api/<RepDailyStockSummary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Daily Stock Summary");
            return Ok(_repDailyStockSummary.GetAll());
        }

        // GET: api/<RepDailyStockSummary>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<RepDailyStockSummary> FilterHeader([FromBody] DateSearchOption criteria)
        {
            return _repDailyStockSummary.GetList(x => x.TransactionDate.HasValue && 
                                                      x.TransactionDate.Value.Date >= criteria.From.Date &&
                                                      x.TransactionDate.Value.Date <= criteria.To.Date);
        }

    }
}
