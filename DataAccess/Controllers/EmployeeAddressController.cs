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
        private readonly IUnitOfWork _unitOfWork;

        public EmployeeAddressController(IRepositoryBase<EmployeeAddress> empAddressRepo,
                                         IUnitOfWork unitOfWork)
        {
            _empAddRepo = empAddressRepo;
            _unitOfWork = unitOfWork;
        }

        // GET api/<EmployeeAddressController>/5
        [HttpGet("{id}")]
        public EmployeeAddress? Get(int id)
        {
            return _empAddRepo.Get(x => x.Gkey == id);
        }

        // POST api/<AddressController>
        [HttpPost("empAddress")]
        public async Task<IActionResult> Post([FromBody] EmployeeAddress value)
        {
            var empAddress = _empAddRepo.Get(x => x.Gkey == value.Gkey);

            if (empAddress == null)
            {
                _empAddRepo.Add(value);
                await _unitOfWork.SaveChangesAsync();
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
                await _unitOfWork.SaveChangesAsync();
                return Ok(empAddress);

            }
        }

        // PUT api/<EmployeeAddressController>/5
        [HttpPut("{id}")]
        public async Task Put(int id, [FromBody] EmployeeAddress value)
        {
            value.Gkey = id;
            _empAddRepo.Update(value);
            await _unitOfWork.SaveChangesAsync();
        }

    }
}
