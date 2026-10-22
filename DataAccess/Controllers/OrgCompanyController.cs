using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrgCompanyController : ControllerBase
    {
        private readonly IRepositoryBase<OrgCompany> _orgCompany;
        private readonly IUnitOfWork _unitOfWork;

        public OrgCompanyController(IRepositoryBase<OrgCompany> orgCompanyRepo,
                                    IUnitOfWork unitOfWork)
        {
            _orgCompany = orgCompanyRepo;
            _unitOfWork = unitOfWork;
        }

        // GET: api/OrgCompany
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_orgCompany.GetAll());
        }

        // PUT: api/OrgCompany/5
        [HttpPut("{gkey:int}")]
        public async Task<IActionResult> Put(int gkey, [FromBody] OrgCompany value)
        {
            if (value.Gkey != gkey)
                return BadRequest("Company GKey in the route does not match the body.");

            _orgCompany.Update(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }
    }
}
