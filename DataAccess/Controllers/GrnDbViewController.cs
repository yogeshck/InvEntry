using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;


namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrnDbViewController :ControllerBase
    {
        private readonly IRepositoryBase<Grndbview> _grnDbViewRepo;
        private readonly ILogger<ProductViewController> _logger;

        public GrnDbViewController(IRepositoryBase<Grndbview> grnDbViewRepo,
                                        ILogger<ProductViewController> logger)
        {
            _grnDbViewRepo = grnDbViewRepo;
            _logger = logger;
        }

        // GET: api/<GrnDbViewController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Stock");
            return Ok(_grnDbViewRepo.GetAll());
        }

        // GET: api/<GrnDbViewController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<Grndbview> FilterHeader([FromBody] DateSearchOption criteria)
        {
            return _grnDbViewRepo.GetList(x => x.GrnDate.HasValue && x.GrnDate.Value.Date >= criteria.From.Date &&
                                                        x.GrnDate.Value.Date <= criteria.To.Date);
        }

    }
}
