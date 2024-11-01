using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrgThisCompanyViewController : ControllerBase
    {
        private readonly IRepositoryBase<OrgThisCompanyView> _orgThisCompanyViewRepo;

        public OrgThisCompanyViewController(IRepositoryBase<OrgThisCompanyView> orgThisCompanyViewRepo)
        {
            _orgThisCompanyViewRepo = orgThisCompanyViewRepo;
        }

        // GET: api/<ProductStockController>
        [HttpGet]
        public OrgThisCompanyView? Get()
        {
            var company = _orgThisCompanyViewRepo.Get(x => x.ThisCompany.HasValue && x.ThisCompany.Value);
            return company;
        }

    }

}
