using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {

        private IRepositoryBase<InvoiceHeader> _invoiceHeaderRepository;
        private IRepositoryBase<InvoiceLine> _invoiceLineRepository;

        public InvoiceController(IRepositoryBase<InvoiceHeader> invoiceHeaderRepository, IRepositoryBase<InvoiceLine> invoiceLineRepository)
        {
            _invoiceHeaderRepository = invoiceHeaderRepository;
            _invoiceLineRepository = invoiceLineRepository;
        }



        // GET: api/<InvoiceController>
        [HttpGet]
        public IEnumerable<InvoiceHeader> GetHeader()
        {
            return _invoiceHeaderRepository.GetAll();
        }

        // GET api/<InvoiceController>/5
        [HttpGet("{invNbr}")]
        public InvoiceHeader? Get(string invNbr)
        {
            return _invoiceHeaderRepository.Get(x => x.InvNbr == invNbr);
        }

        // POST api/<InvoiceController>
        [HttpPost]
        public void Post([FromBody] InvoiceHeader value)
        {
            _invoiceHeaderRepository.Add(value);
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
