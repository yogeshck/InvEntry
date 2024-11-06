using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private string DocumentPrefixFormat = "B{0}";

        private IRepositoryBase<InvoiceHeader> _invoiceHeaderRepository;
        private readonly IRepositoryBase<VoucherType> _voucherTypeRepo;
    //    private IRepositoryBase<OrgCompany> _orgCompanyRepository;

        public InvoiceController(   IRepositoryBase<InvoiceHeader> invoiceHeaderRepository,
                                    IRepositoryBase<VoucherType> voucherTypeRepo )  
                                   // IRepositoryBase<OrgCompany> orgCompanyRepository        )
        {
            _invoiceHeaderRepository = invoiceHeaderRepository;
            _voucherTypeRepo = voucherTypeRepo;
           // _orgCompanyRepository = orgCompanyRepository;
        }

        // GET: api/<InvoiceController>
        [HttpGet]
        public IEnumerable<InvoiceHeader> GetHeader()
        {
            return _invoiceHeaderRepository.GetAll();
        }

        // GET: api/<InvoiceController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<InvoiceHeader> FilterHeader([FromBody] InvoiceSearchOption criteria)
        {
            return _invoiceHeaderRepository.GetList(x => x.InvDate.HasValue && x.InvDate.Value.Date >= criteria.From.Date &&
                                                        x.InvDate.Value.Date <= criteria.To.Date);
        }

        // GET api/<InvoiceController>/5
        [HttpGet("{invNbr}")]
        public InvoiceHeader? Get(string invNbr)
        {
            return _invoiceHeaderRepository.Get(x => x.InvNbr == invNbr);
        }

        // POST api/<InvoiceController>
        [HttpPost]
        public InvoiceHeader Post([FromBody] InvoiceHeader value)
        {

            //var company = _orgCompanyRepository.Get(x => x.ThisCompany.HasValue && x.ThisCompany.Value);
            //company.InvId++;
            //_orgCompanyRepository.Update(company);
            //value.InvNbr = string.Format(InvoicePrefixFormat, company?.InvId?.ToString("D4"));

            var voucherType = _voucherTypeRepo.Get(x => x.DocumentType == "Sale Invoice"); // value.VoucherType);

            voucherType.LastUsedNumber++;

            _voucherTypeRepo.Update(voucherType);

            DocumentPrefixFormat = voucherType.DocNbrPrefix;

            value.InvNbr = string.Format("{0}{1}",DocumentPrefixFormat, voucherType?.LastUsedNumber?.ToString("D4"));

            _invoiceHeaderRepository.Add(value);
            return value;
        }

        // PUT api/<InvoiceController>/5
        [HttpPut("{invNbr}")]
        public void Put(string invNbr, [FromBody] InvoiceHeader value)
        {
            value.InvNbr = invNbr;
            _invoiceHeaderRepository.Update(value);
        }

        // DELETE api/<InvoiceController>/5
        [HttpDelete("{invNbr}")]
        public void Delete(string invNbr)
        {
            _invoiceHeaderRepository.Remove(Get(invNbr));
        }
    }
}
