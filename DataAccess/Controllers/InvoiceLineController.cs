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
        private readonly IUnitOfWork _unitOfWork;

        public InvoiceLineController(IRepositoryBase<InvoiceLine> invoiceLineRepository,
                                     IUnitOfWork unitOfWork)
        {
            _invoiceLineRepository = invoiceLineRepository;
            _unitOfWork = unitOfWork;
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
        public async Task<InvoiceLine> Post([FromBody] InvoiceLine value)
        {
            value.CreatedOn = DateTime.Now;
            _invoiceLineRepository.Add(value);
            await _unitOfWork.SaveChangesAsync();
            return value;
        }

        // PUT api/<InvoiceController>/5
        [HttpPut("{invNbr}")]
        public async Task Put(int invNbr, [FromBody] InvoiceLine value)
        {
            value.InvLineNbr = invNbr;
            value.ModifiedOn = DateTime.Now;
            _invoiceLineRepository.Update(value);
            await _unitOfWork.SaveChangesAsync();
        }

        // DELETE api/<InvoiceController>/5
        [HttpDelete("{invNbr}")]
        public async Task Delete(long invNbr)
        {
            var line = Get(invNbr);

            if (line is not null)
            {
                _invoiceLineRepository.Remove(line);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
