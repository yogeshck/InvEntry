using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MtblVoucherTypeController : ControllerBase
    {
        private readonly IRepositoryBase<MtblVoucherType> _mtblVoucherTypeRepo;

        public MtblVoucherTypeController(IRepositoryBase<MtblVoucherType> mtblVoucherTypeRepo)
        {
            _mtblVoucherTypeRepo = mtblVoucherTypeRepo;
        }

        // GET: api/<MtblVoucherTypeController>
        [HttpGet]
        public IEnumerable<MtblVoucherType> Get()
        {
            return _mtblVoucherTypeRepo.GetAll();
        }

        // GET api/<MtblVoucherTypeController>/5
        [HttpGet("{voucherTypeName}")]
        public IEnumerable<MtblVoucherType> Get(string voucherTypeName)
        {
            return _mtblVoucherTypeRepo.GetList(x => (x.VoucherType == voucherTypeName));
        }

    }
}
