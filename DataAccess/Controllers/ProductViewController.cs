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

        public ProductViewController(   IRepositoryBase<ProductView> productStockViewRepo, 
                                        ILogger<ProductViewController> logger)
        {
            _productStkViewRepo = productStockViewRepo;
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
        [HttpGet("{productId}")]
        public async Task<IActionResult> Get(string productId)
        {
            return Ok(_productStkViewRepo.Get(x => x.Id == productId));
        }

        // GET api/<ProductViewController>/5
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetCategory(string category)
        {
            return Ok(_productStkViewRepo.GetList(x => x.Category == category && x.BalanceWeight > 0));
        }

        // GET api/<ProductViewController>/5
        [HttpGet("productSku/{productSku}")]
        public async Task<IActionResult> GetByProductSku(string productSku)
        {
            return Ok(_productStkViewRepo.Get(x => x.ProductSku == productSku));
        }

    }
}
