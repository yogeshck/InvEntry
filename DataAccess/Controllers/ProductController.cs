﻿using DataAccess.Models;
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
        
        public ProductController(
                                    IRepositoryBase<Product> productRepo, 
                                    ILogger<ProductController> stkLogger      )
        {
            _product = productRepo;
            _logger = stkLogger;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Product Stock");
            return Ok(_product.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{productId}")]
        public async Task<IActionResult> Get(string productId)
        {
            return Ok(_product.Get(x => x.Id == productId));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public void Post([FromBody] Product value)
        {
            _product.Add(value);
        }

        // PUT api/<ProductStockController>/5
        [HttpPut("{productGkey}")]
        public void Put(decimal productGkey, [FromBody] Product value)
        {
            _product.Update(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{productGkey}")]
        public async Task<IActionResult> Delete(decimal productGkey)
        {
            var product = _product.Get(x => x.Gkey == productGkey);

            if (product is not null)
                _product.Remove(product);

            return Ok();
        }
    }
}
