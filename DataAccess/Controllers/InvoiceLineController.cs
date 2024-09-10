using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceLineController : ControllerBase
    {
        private IRepositoryBase<InvoiceLine> _invoiceLineRepository;

        public InvoiceLineController(IRepositoryBase<InvoiceLine> invoiceLineRepository)
        {
            _invoiceLineRepository = invoiceLineRepository;
        }



        // GET: api/<InvoiceController>
        [HttpGet]
        public IEnumerable<InvoiceLine> GetHeader()
        {
            return _invoiceLineRepository.GetAll();
        }

        // GET api/<InvoiceController>/5
        [HttpGet("{invNbr}")]
        public InvoiceLine? Get(long invNbr)
        {
            return _invoiceLineRepository.Get(x => x.InvLineNbr == invNbr);
        }

        // POST api/<InvoiceController>
        [HttpPost]
        public void Post([FromBody] InvoiceLine value)
        {
            _invoiceLineRepository.Add(value);
        }

        // PUT api/<InvoiceController>/5
        [HttpPut("{invNbr}")]
        public void Put(long invNbr, [FromBody] InvoiceLine value)
        {
            value.InvLineNbr = invNbr;
            _invoiceLineRepository.Update(value);
        }

        // DELETE api/<InvoiceController>/5
        [HttpDelete("{invNbr}")]
        public void Delete(long invNbr)
        {
            _invoiceLineRepository.Remove(Get(invNbr));
        }
    }
}
