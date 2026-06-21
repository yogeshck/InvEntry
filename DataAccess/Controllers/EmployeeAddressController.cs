using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeAddressController : ControllerBase
    {
        private readonly IRepositoryBase<EmployeeAddress> _empAddRepo;

        public EmployeeAddressController(IRepositoryBase<EmployeeAddress> empAddressRepo)
        {
            _empAddRepo = empAddressRepo;
        }

        // GET api/<EmployeeAddressController>/5
        [HttpGet("{id}")]
        public EmployeeAddress? Get(int id)
        {
            return _empAddRepo.Get(x => x.Gkey == id);
        }

        // POST api/<AddressController>
        [HttpPost("empAddress")]
        public IActionResult Post([FromBody] EmployeeAddress value)
        {
            var empAddress = _empAddRepo.Get(x => x.Gkey == value.Gkey);

            if (empAddress == null)
            {
                _empAddRepo.Add(value);
                return Ok(value);
            }
            else
            {
                empAddress.AddressLine1 = value.AddressLine1;
                empAddress.AddressLine2 = value.AddressLine2;
                empAddress.AddressLine3 = value.AddressLine3;
                empAddress.City = value.City;
                empAddress.District = value.District;
                empAddress.State = value.State;
                empAddress.Country = value.Country;
                empAddress.Pincode = value.Pincode;
                empAddress.IsPrimary = value.IsPrimary;
                empAddress.UpdatedBy = value.UpdatedBy;
                empAddress.UpdatedOn = value.UpdatedOn;
                empAddress.CreatedBy = value.CreatedBy;
                empAddress.CreatedOn = value.CreatedOn;

                _empAddRepo.Update(empAddress);
                return Ok(empAddress);

            }
        }

        // PUT api/<EmployeeAddressController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] EmployeeAddress value)
        {
            value.Gkey = id;
            _empAddRepo.Update(value);
        }

    }
}
