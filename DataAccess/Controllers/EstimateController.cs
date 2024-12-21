using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Models;


namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EstimateController : ControllerBase
    {
        private string EstimatePrefixFormat = "E{0}";

        private IRepositoryBase<EstimateHeader> _estimateHeaderRepository;
        //private IRepositoryBase<OrgCompany> _orgCompanyRepository;
        private readonly IRepositoryBase<VoucherType> _voucherTypeRepo;

        public EstimateController(IRepositoryBase<EstimateHeader> estimateHeaderRepository,
                                                IRepositoryBase<VoucherType> voucherTypeRepo)
                                 // IRepositoryBase<OrgCompany> orgCompanyRepository)
        {
            _estimateHeaderRepository = estimateHeaderRepository;
            _voucherTypeRepo = voucherTypeRepo;
           // _orgCompanyRepository = orgCompanyRepository;
        }



        // GET: api/<EstimateController>
        [HttpGet]
        public IEnumerable<EstimateHeader> GetHeader()
        {
            return _estimateHeaderRepository.GetAll();
        }

        // GET: api/<InvoiceController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<EstimateHeader> FilterHeader([FromBody] DateSearchOption criteria)
        {
            return _estimateHeaderRepository.GetList(x => x.EstDate.HasValue && x.EstDate.Value.Date >= criteria.From.Date &&
                                                        x.EstDate.Value.Date <= criteria.To.Date);
        }

        // GET api/<InvoiceController>/5
        [HttpGet("{estNbr}")]
        public EstimateHeader? Get(string estNbr)
        {
            return _estimateHeaderRepository.Get(x => x.EstNbr == estNbr);
        }

        // POST api/<InvoiceController>
        [HttpPost]
        public EstimateHeader Post([FromBody] EstimateHeader value)
        {

            var voucherType = _voucherTypeRepo.Get(x => x.DocumentType == "Estimate"); // value.VoucherType);

            voucherType.LastUsedNumber++;

            _voucherTypeRepo.Update(voucherType);

            EstimatePrefixFormat = voucherType.DocNbrPrefix;

            value.EstNbr = string.Format("{0}{1}", EstimatePrefixFormat, voucherType?.LastUsedNumber?.ToString($"D{voucherType.DocNbrLength}"));

            _estimateHeaderRepository.Add(value);

            return value;
        }

        // PUT api/<InvoiceController>/5
        [HttpPut("{estNbr}")]
        public void Put(string estNbr, [FromBody] EstimateHeader value)
        {
            value.EstNbr = estNbr;
            _estimateHeaderRepository.Update(value);
        }

        // DELETE api/<EstimateController>/5
        [HttpDelete("{estNbr}")]
        public void Delete(string estNbr)
        {
            _estimateHeaderRepository.Remove(Get(estNbr));
        }
    }
}
