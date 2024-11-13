using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductViewController : ControllerBase
    {
        private readonly IRepositoryBase<ProductView> _productStkViewRepo;
        private readonly ILogger<ProductViewController> _logger;

        public ProductViewController(   IRepositoryBase<ProductView> _productStockViewRepo, 
                                        ILogger<ProductViewController> logger)
        {
            _productStkViewRepo = _productStockViewRepo;
            _logger = logger;
        }

        // GET: api/<ProductViewController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Stock");
            return Ok(_productStkViewRepo.GetAll());
        }

        // GET api/<ProductViewController>/5
        [HttpGet("{productSku}")]
        public async Task<IActionResult> Get(string productSku)
        {
            return Ok(_productStkViewRepo.Get(x => x.ProductSku == productSku));
        }

    }
}
