using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;


namespace DataAccess.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        public class RepSalesInvrctDbViewController : ControllerBase
        {
            private readonly IRepositoryBase<RepSalesInvrctDbView> _repSalesInvRctDbViewRepo;
            private readonly ILogger<RepSalesInvrctDbViewController> _logger;

            public RepSalesInvrctDbViewController(IRepositoryBase<RepSalesInvrctDbView> repSalesInvRctDbViewRepo,
                                            ILogger<RepSalesInvrctDbViewController> logger)
            {
                _repSalesInvRctDbViewRepo = repSalesInvRctDbViewRepo;
                _logger = logger;
            }

            // GET: api/<GrnDbViewController>
            [HttpGet]
            public async Task<IActionResult> GetAll()
            {
                _logger.LogInformation("All Product Stock");
                return Ok(_repSalesInvRctDbViewRepo.GetAll());
            }

            // GET: api/<GrnDbViewController>/24-Sep-2024/25-Sep-2024
            [HttpPost("filter")]
            public IEnumerable<RepSalesInvrctDbView> FilterHeader([FromBody] DateSearchOption criteria)
            {
                return _repSalesInvRctDbViewRepo.GetList(x => x.InvDate.HasValue && x.InvDate.Value.Date >= criteria.From.Date &&
                                                            x.InvDate.Value.Date <= criteria.To.Date);
            }

        }
    
}
