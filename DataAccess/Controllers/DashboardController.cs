using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {

        private readonly IRepositoryBase<Supplier> _supplier;

        public DashboardController(IRepositoryBase<Supplier> supplierRepo)
        {
            _supplier = supplierRepo;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetAll()
        {
            return Ok();
        }
    }
}
