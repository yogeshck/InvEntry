using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceArReceiptController : ControllerBase
    {

        private readonly IRepositoryBase<InvoiceArReceipt> _invoiceArReceipt;
        private readonly IUnitOfWork _unitOfWork;

        public InvoiceArReceiptController(IRepositoryBase<InvoiceArReceipt> invoiceArReceiptRepo,
                                          IUnitOfWork unitOfWork)
        {
            _invoiceArReceipt = invoiceArReceiptRepo;
            _unitOfWork = unitOfWork;
        }

        // GET: api/<ARInvoiceReceiptController>
        [HttpGet]
        public IEnumerable<InvoiceArReceipt> Get()
        {
            return _invoiceArReceipt.GetAll();
        }

        // GET api/<ARInvoiceReceiptController>/5
/*        [HttpGet("{invoiceNbr}")]
        public InvoiceArReceipt? Get(string invoiceNbr)
        {
            return _invoiceArReceipt.Get(x => x.InvoiceNbr == invoiceNbr);
        }
*/
        // GET api/<GrnLineSummaryController>/5
        [HttpGet("{hdrGkey}")]
        public IEnumerable<InvoiceArReceipt> GetByInvHdrGKey(int hdrGkey, [FromQuery] string? invNbr = null)
        {
            // Invoice and estimate headers live in different tables and their gkeys overlap,
            // so the gkey alone can pull in another document's receipts.
            if (!string.IsNullOrWhiteSpace(invNbr))
            {
                return _invoiceArReceipt.GetList(x => x.InvoiceGkey == hdrGkey && x.InvoiceNbr == invNbr)
                    .OrderBy(x => x.SeqNbr);
            }

            return _invoiceArReceipt.GetList(x => x.InvoiceGkey == hdrGkey)
                .OrderBy(x => x.SeqNbr);
        }

        // POST api/<ARInvoiceReceiptController>
        [HttpPost]
        public async Task<InvoiceArReceipt> Post([FromBody] InvoiceArReceipt value)
        {
            _invoiceArReceipt.Add(value);
            await _unitOfWork.SaveChangesAsync();
            return value;
        }

        // PUT api/<ARInvoiceReceiptController>/5
        [HttpPut]
        public async Task Put([FromBody] InvoiceArReceipt value)
        {
            _invoiceArReceipt.Update(value);
            await _unitOfWork.SaveChangesAsync();
        }

        // DELETE api/<ARInvoiceReceiptController>/5
        [HttpDelete("{invoiceNbr}")]
        public async Task Delete(string invoiceNbr)
        {

            var arInvoiceReceipt = _invoiceArReceipt.Get(x => x.InvoiceNbr == invoiceNbr);

            if (arInvoiceReceipt is not null)
            {
                _invoiceArReceipt.Remove(arInvoiceReceipt);
                await _unitOfWork.SaveChangesAsync();
            }


        }
    }


}
