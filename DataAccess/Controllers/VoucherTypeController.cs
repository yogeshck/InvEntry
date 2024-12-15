using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherTypeController : ControllerBase
    {
        private readonly IRepositoryBase<VoucherType> _voucherType;

        public VoucherTypeController(IRepositoryBase<VoucherType> voucherTypeRepo)
        {
            _voucherType = voucherTypeRepo;
        }

        // GET: api/<VoucherTypeController>
        [HttpGet]
        public IEnumerable<VoucherType> Get()
        {
            return _voucherType.GetAll();
        }

        // GET api/<VoucherTypeController>/5
        [HttpGet("{documenType}")]
        public IEnumerable<VoucherType> Get(string documenType)
        {
            return _voucherType.GetList(x => (x.DocumentType == documenType));
        }

        // POST api/<VoucherTypeController>
        [HttpPost]
        public VoucherType Post([FromBody] VoucherType value)
        {
            _voucherType.Add(value);
            return value;
        }

        // PUT api/<VoucherTypeController>/5
        [HttpPut]
        public IActionResult Put([FromBody] VoucherType value)
        {
            _voucherType.Update(value);
            return Ok(value);
        }

        // DELETE api/<VoucherTypeController>/5
        [HttpDelete("{documenType}")]
        public void Delete(string documenType)
        {

            var vcherType = _voucherType.Get(x => x.DocumentType == documenType);

            if (vcherType is not null)
                _voucherType.Remove(vcherType);

        }
    }

}
