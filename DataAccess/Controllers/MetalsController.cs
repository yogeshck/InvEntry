using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetalsController : ControllerBase
    {
        private readonly IRepositoryBase<Metal> _metalsRepo;
        private readonly IUnitOfWork _unitOfWork;

        public MetalsController(IRepositoryBase<Metal> metalsRepo,
                                IUnitOfWork unitOfWork)
        {
            _metalsRepo = metalsRepo;
            _unitOfWork = unitOfWork;
        }


        // GET: api/<ProductStockController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(_metalsRepo.GetAll());
        }

        // GET api/<ProductStockController>/5
        [HttpGet("{metalName}")]
        public async Task<IActionResult> Get(string metalName)
        {
            return Ok(_metalsRepo.Get(x => x.MetalName == metalName));
        }

        // POST api/<ProductStockController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Metal value)
        {
            _metalsRepo.Add(value);
            await _unitOfWork.SaveChangesAsync();

            return Ok(value);
        }


        // PUT api/<ProductStockController>/5
        [HttpPut("{metalGkey}")]
        public async Task<IActionResult> Put(decimal metalGkey, [FromBody] Metal value)
        {
            _metalsRepo.Update(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

        // DELETE api/<ProductStockController>/5
        [HttpDelete("{metalGkey}")]
        public async Task<IActionResult> Delete(string metalName)
        {
            var product = _metalsRepo.Get(x => x.MetalName == metalName);

            if (product is not null)
            {
                _metalsRepo.Remove(product);
                await _unitOfWork.SaveChangesAsync();
            }

            return Ok();
        }
    }

}

