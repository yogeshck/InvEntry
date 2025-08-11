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

        public InvoiceArReceiptController(IRepositoryBase<InvoiceArReceipt> invoiceArReceiptRepo)
        {
            _invoiceArReceipt = invoiceArReceiptRepo;
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
        public IEnumerable<InvoiceArReceipt> GetByInvHdrGKey(int hdrGkey)
        {
            return _invoiceArReceipt.GetList(x => x.InvoiceGkey == hdrGkey)
                .OrderBy(x => x.SeqNbr);
        }

        // POST api/<ARInvoiceReceiptController>
        [HttpPost]
        public InvoiceArReceipt Post([FromBody] InvoiceArReceipt value)
        {
            _invoiceArReceipt.Add(value);
            return value;
        }

        // PUT api/<ARInvoiceReceiptController>/5
        [HttpPut]
        public void Put([FromBody] InvoiceArReceipt value)
        {
            _invoiceArReceipt.Update(value);
        }

        // DELETE api/<ARInvoiceReceiptController>/5
        [HttpDelete("{invoiceNbr}")]
        public void Delete(string invoiceNbr)
        {

            var arInvoiceReceipt = _invoiceArReceipt.Get(x => x.InvoiceNbr == invoiceNbr);

            if (arInvoiceReceipt is not null)
                _invoiceArReceipt.Remove(arInvoiceReceipt);


        }
    }


}
