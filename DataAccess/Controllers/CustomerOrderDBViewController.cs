using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;


namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerOrderDBViewController : ControllerBase
    {
        private readonly IRepositoryBase<CustomerOrderDbView> _custOrderDbViewRepo;
        private readonly ILogger<CustomerOrderDBViewController> _logger;

        public CustomerOrderDBViewController(IRepositoryBase<CustomerOrderDbView> custOrderDbViewRepo,
                                            ILogger<CustomerOrderDBViewController> logger)
        {
            _custOrderDbViewRepo = custOrderDbViewRepo;
            _logger = logger;
        }

        // GET: api/<GrnDbViewController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Stock");
            return Ok(_custOrderDbViewRepo.GetAll());
        }

        // GET: api/<GrnDbViewController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<CustomerOrderDbView> FilterHeader([FromBody] DateSearchOption criteria)
        {
            return _custOrderDbViewRepo.GetList(x => x.OrderDate.HasValue && x.OrderDate.Value.Date >= criteria.From.Date &&
                                                        x.OrderDate.Value.Date <= criteria.To.Date);
        }
    }

}