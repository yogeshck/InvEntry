using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrnController : ControllerBase
    {
        private string DocumentPrefixFormat = "";

        private IRepositoryBase<GrnHeader> _grnHeaderRepository;
        private readonly IRepositoryBase<VoucherType> _voucherTypeRepo;

        public GrnController( IRepositoryBase<GrnHeader> grnHeaderRepository,
                              IRepositoryBase<VoucherType> voucherTypeRepo)
        {
            _grnHeaderRepository = grnHeaderRepository;
            _voucherTypeRepo = voucherTypeRepo; 
        }

        // GET: api/<GrnController>
        [HttpGet]
        public IEnumerable<GrnHeader> GetHeader()
        {
            return _grnHeaderRepository.GetAll();
        }

        // GET: api/<GrnController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<GrnHeader> FilterHeader([FromBody] DateSearchOption criteria)
        {
            return _grnHeaderRepository.GetList(x => x.GrnDate.HasValue && x.GrnDate.Value.Date >= criteria.From.Date &&
                                                        x.GrnDate.Value.Date <= criteria.To.Date);
        }

        // GET api/<GrnController>/5
        [HttpGet("{grnNbr}")]
        public GrnHeader? Get(string grnNbr)
        {
            return _grnHeaderRepository.Get(x => x.GrnNbr == grnNbr);
        }

        // GET api/<GrnController>/5
        [HttpGet("supplierId/{supplierId}")]
        public async Task<IActionResult> GetBySupplierId(string supplierId)
        {
            return Ok(_grnHeaderRepository.GetList(x => x.SupplierId == supplierId && x.Status == "Open"));
        }


        // POST api/<GrnController>
        [HttpPost]
        public GrnHeader Post([FromBody] GrnHeader value)
        {

            var voucherType = _voucherTypeRepo.Get(x => x.DocumentType == "GRN");  

            voucherType.LastUsedNumber++;

            _voucherTypeRepo.Update(voucherType);

            DocumentPrefixFormat = voucherType.DocNbrPrefix;

            value.GrnNbr = string.Format("{0}{1}", DocumentPrefixFormat, voucherType?.LastUsedNumber?.ToString("D4"));

            _grnHeaderRepository.Add(value);
            return value;
        }

        // PUT api/<GrnController>/5
        [HttpPut("{grnNbr}")]
        public void Put(string grnNbr, [FromBody] GrnHeader value)
        {
            value.GrnNbr = grnNbr;
            _grnHeaderRepository.Update(value);
        }

        // DELETE api/<GrnController>/5
        [HttpDelete("{grnNbr}")]
        public void Delete(string grnNbr)
        {
            _grnHeaderRepository.Remove(Get(grnNbr));
        }
    }
}
