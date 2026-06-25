using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {

        private readonly IRepositoryBase<Supplier> _supplier;

        public SupplierController(IRepositoryBase<Supplier> supplierRepo)
        {
            _supplier = supplierRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(_supplier.GetAll());
        }

        [HttpGet("phone/{phone}")]
        public async Task<IActionResult> GetByPhone(string phone)
        {
            return Ok(_supplier.Get(x => x.Phone == phone));
        }


        [HttpGet("{gkey}")]
        public async Task<IActionResult> Get(int gkey)
        {
            return Ok(_supplier.Get(x => x.Gkey == gkey));
        }

        [HttpPost]
        public Supplier Post([FromBody] Supplier value)
        {
            var supplier = _supplier.Get(x => x.Phone == value.Phone);

            if (supplier == null)
            {
                _supplier.Add(value);
                supplier = _supplier.Get(x => x.Phone == value.Phone);
            }
            else
            {
                supplier.SupplierName = value.SupplierName;
                // supplier.Address = value.Address;
                _supplier.Update(supplier);
            }

            return supplier ?? value;
        }

/*        [HttpPut("{phone}")]
        public void Put(string phone, [FromBody] Supplier value)
        {
            _supplier.Update(value);

        }*/
        
        // DELETE api/<ProductStockController>/5
        [HttpDelete("{MobileNbr}")]
        public async Task<IActionResult> Delete(string MobileNbr)
            {
                var product = _supplier.Get(x => x.Phone == MobileNbr);

                if (product is not null)
                    _supplier.Remove(product);

                return Ok();
            }


        }
    }

