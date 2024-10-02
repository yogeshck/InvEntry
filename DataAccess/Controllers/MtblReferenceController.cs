using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        public class MtblReferenceController : ControllerBase
        {

            private readonly IRepositoryBase<MtblReference> _mtblReference;

            public MtblReferenceController(IRepositoryBase<MtblReference> mtblReferenceRepo)
            {
                _mtblReference = mtblReferenceRepo;
            }

            // GET: api/<MtblReferenceController>
            [HttpGet]
            public IEnumerable<MtblReference> Get()
            {
                return _mtblReference.GetAll();
            }

            // GET api/<MtblReferenceController>/5
            public string RefCode { get; set; } = null!;
            [HttpGet("{refName}/{refCode}")]
            public MtblReference? Get(string refName, string refCode)
            {
                return _mtblReference.Get(x => (x.RefName == refName && x.RefCode == refCode));
            }

            // POST api/<MtblReferenceController>
            [HttpPost]
            public MtblReference Post([FromBody] MtblReference value)
            {
                _mtblReference.Add(value);
                return value;
            }

            // PUT api/<MtblReferenceController>/5
            [HttpPut]
            public void Put([FromBody] MtblReference value)
            {
                _mtblReference.Update(value);
            }

            // DELETE api/<MtblReferenceController>/5
/*            [HttpDelete("{voucherNbr}")]
            public void Delete(string voucherNbr)
            {

                var finDayBook = _mtblReference.Get(x => x.VoucherNbr == voucherNbr);

                if (finDayBook is not null)
                    _mtblReference.Remove(finDayBook);


            }*/
        }
    

}
