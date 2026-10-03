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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DailyStockSummaryController> _logger;

        public DailyStockSummaryController(
            IRepositoryBase<DailyStockSummary> dailyStockSummaryRepo,
            IUnitOfWork unitOfWork,
            ILogger<DailyStockSummaryController> logger)
        {
            _dailyStockSummary = dailyStockSummaryRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: api/dailyStockSummary
        [HttpGet]
        public IActionResult GetAll()
        {
            _logger.LogInformation("All Product Daily Stock Summary");

            return Ok(_dailyStockSummary.GetAll());
        }

        // POST: api/dailyStockSummary/filter
        [HttpPost("filter")]
        public IEnumerable<DailyStockSummary> FilterHeader(
            [FromBody] DateSearchOption criteria)
        {
            return _dailyStockSummary
                .GetList(x =>
                    x.TransactionDate.HasValue &&
                    x.TransactionDate.Value.Date >= criteria.From.Date &&
                    x.TransactionDate.Value.Date <= criteria.To.Date)
                .OrderBy(x => x.TransactionDate)
                .ThenBy(x => x.Metal);
        }

        // POST: api/dailyStockSummary
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromBody] DailyStockSummary value)
        {
            _dailyStockSummary.Add(value);

            await _unitOfWork.SaveChangesAsync();

            return Ok(value);
        }

        // PUT: api/dailyStockSummary/5
        [HttpPut("{gkey:int}")]
        public async Task<IActionResult> Put(
            int gkey,
            [FromBody] DailyStockSummary value)
        {
            if (gkey != value.Gkey)
            {
                return BadRequest(
                    "Daily stock summary key does not match.");
            }

            _dailyStockSummary.Update(value);

            await _unitOfWork.SaveChangesAsync();

            return Ok(value);
        }
    }
}