using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IRepositoryBase<Product> _product;
        private readonly ILogger<ProductController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        
        public ProductController(
                                    IRepositoryBase<Product> productRepo, 
                                    ILogger<ProductController> stkLogger,
                                    IUnitOfWork unitOfWork)
        {
            _product = productRepo;
            _logger = stkLogger;
            _unitOfWork = unitOfWork;
        }

        // GET: api/<ProductController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Info");
            return Ok(_product.GetAll());
        }

        [HttpGet("gkey/{productGkey:int}")]
        public IActionResult GetByGkey(int productGkey)
        {
            if (productGkey <= 0)
                return BadRequest("Product GKey must be greater than zero.");

            var product = _product.Get(x => x.Gkey == productGkey);
            return product is null
                ? NotFound($"PRODUCT GKey {productGkey} was not found.")
                : Ok(product);
        }

        // GET api/<ProductController>/5
        [HttpGet("{productId}")]
        public async Task<IActionResult> Get(string productId)
        {
            return Ok(_product.Get(x => x.Id == productId));
        }

        // POST api/<ProductController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Product value)
        {
            _product.Add(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

        // PUT api/<ProductController>/5
        [HttpPut("{productGkey}")]
        public async Task<IActionResult> Put(decimal productGkey, [FromBody] Product value)
        {
            _product.Update(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

        // DELETE api/<ProductController>/5
        [HttpDelete("{productGkey}")]
        public async Task<IActionResult> Delete(decimal productGkey)
        {
            var product = _product.Get(x => x.Gkey == productGkey);

            if (product is not null)
            {
                _product.Remove(product);
                await _unitOfWork.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
