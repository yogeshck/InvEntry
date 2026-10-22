using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;
using DataAccess.Models;
using DataAccess.Workflows;
using InvEntry.Contracts.Estimates;


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
        private readonly IEstimateWorkflow _estimateWorkflow;
        private readonly IUnitOfWork _unitOfWork;

        public EstimateController(IRepositoryBase<EstimateHeader> estimateHeaderRepository,
                                                IRepositoryBase<VoucherType> voucherTypeRepo,
                                                IEstimateWorkflow estimateWorkflow,
                                                IUnitOfWork unitOfWork)
                                 // IRepositoryBase<OrgCompany> orgCompanyRepository)
        {
            _estimateHeaderRepository = estimateHeaderRepository;
            _voucherTypeRepo = voucherTypeRepo;
            _estimateWorkflow = estimateWorkflow;
            _unitOfWork = unitOfWork;
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

        [HttpPost("save")]
        public async Task<ActionResult<SaveEstimateResponse>> Save(
            [FromBody] SaveEstimateRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _estimateWorkflow.SaveAsync(request, cancellationToken));
        }

        // POST api/<InvoiceController>
        [HttpPost]
        public async Task<EstimateHeader> Post([FromBody] EstimateHeader value)
        {

            var voucherType = _voucherTypeRepo.Get(x => x.DocumentType == "Estimate"); // value.VoucherType);

            voucherType.LastUsedNumber++;

            _voucherTypeRepo.Update(voucherType);

            EstimatePrefixFormat = voucherType.DocNbrPrefix;

            value.EstNbr = string.Format("{0}{1}", EstimatePrefixFormat, voucherType?.LastUsedNumber?.ToString($"D{voucherType.DocNbrLength}"));

            _estimateHeaderRepository.Add(value);
            await _unitOfWork.SaveChangesAsync();

            return value;
        }

        // PUT api/<InvoiceController>/5
        [HttpPut("{estNbr}")]
        public async Task Put(string estNbr, [FromBody] EstimateHeader value)
        {
            value.EstNbr = estNbr;
            _estimateHeaderRepository.Update(value);
            await _unitOfWork.SaveChangesAsync();
        }

        // DELETE api/<EstimateController>/5
        [HttpDelete("{estNbr}")]
        public async Task Delete(string estNbr)
        {
            var estimate = Get(estNbr);

            if (estimate is not null)
            {
                _estimateHeaderRepository.Remove(estimate);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
