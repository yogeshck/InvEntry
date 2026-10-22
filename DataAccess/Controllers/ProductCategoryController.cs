using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCategoryController : ControllerBase
    {
        private readonly IRepositoryBase<ProductCategory> _productCategory;
        private readonly IUnitOfWork _unitOfWork;

        public ProductCategoryController(IRepositoryBase<ProductCategory> productCategoryRepo,
                                         IUnitOfWork unitOfWork)
        {
            _productCategory = productCategoryRepo;
            _unitOfWork = unitOfWork;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(_productCategory.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{categoryName}")]
        public async Task<IActionResult> Get(string categoryName)
        {
            return Ok(_productCategory.Get(x => x.Name == categoryName));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProductCategory value)
        {
             _productCategory.Add(value);
             await _unitOfWork.SaveChangesAsync();

            return Ok(value);
        }

        // PUT api/<ProductStockController>/5
        [HttpPut("{productGkey}")]
        public async Task<IActionResult> Put(decimal productGkey, [FromBody] ProductCategory value)
        {
            _productCategory.Update(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{productGkey}")]
        public async Task<IActionResult> Delete(string categoryName)
        {
            var product = _productCategory.Get(x => x.Name == categoryName);

            if(product is not null)
            {
                _productCategory.Remove(product);
                await _unitOfWork.SaveChangesAsync();
            }

            return Ok();
        }
    }
    
}
