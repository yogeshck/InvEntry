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
            private readonly IUnitOfWork _unitOfWork;

            public MtblReferenceController(IRepositoryBase<MtblReference> mtblReferenceRepo,
                                           IUnitOfWork unitOfWork)
            {
                _mtblReference = mtblReferenceRepo;
                _unitOfWork = unitOfWork;
            }

            // GET: api/<MtblReferenceController>
            [HttpGet]
            public IEnumerable<MtblReference> Get()
            {
                return _mtblReference.GetAll();
            }

            // GET api/<MtblReferenceController>/5
            [HttpGet("{refName}")]
            public IEnumerable<MtblReference> Get(string refName)
            {
                return _mtblReference.GetList(x => (x.RefName == refName ))
                                         .OrderBy(x => x.SortSeq);
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
            public async Task<MtblReference> Post([FromBody] MtblReference value)
            {
                _mtblReference.Add(value);
                await _unitOfWork.SaveChangesAsync();
                return value;
            }

            // PUT api/<MtblReferenceController>/5
            [HttpPut]
            public async Task Put([FromBody] MtblReference value)
            {
                _mtblReference.Update(value);
                await _unitOfWork.SaveChangesAsync();
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
