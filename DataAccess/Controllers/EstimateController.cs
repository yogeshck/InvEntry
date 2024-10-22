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
        private string EstimatePrefixFormat = "B{0}";

        private IRepositoryBase<EstimateHeader> _estimateHeaderRepository;
        private IRepositoryBase<OrgCompany> _orgCompanyRepository;

        public EstimateController(IRepositoryBase<EstimateHeader> estimateHeaderRepository, IRepositoryBase<OrgCompany> orgCompanyRepository)
        {
            _estimateHeaderRepository = estimateHeaderRepository;
            _orgCompanyRepository = orgCompanyRepository;
        }



        // GET: api/<EstimateController>
        [HttpGet]
        public IEnumerable<EstimateHeader> GetHeader()
        {
            return _estimateHeaderRepository.GetAll();
        }

        // GET: api/<InvoiceController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<EstimateHeader> FilterHeader([FromBody] EstimateSearchOption criteria)
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
            var company = _orgCompanyRepository.Get(x => x.ThisCompany.HasValue && x.ThisCompany.Value);

            company.DraftId++;

            _orgCompanyRepository.Update(company);

            value.EstNbr = string.Format(EstimatePrefixFormat, company?.InvId?.ToString("D4"));

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
