using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class ARInvoiceReceiptController : ControllerBase
        {

            private readonly IRepositoryBase<ArInvoiceReceipt> _arInvoiceReceipt;

            public ARInvoiceReceiptController(IRepositoryBase<ArInvoiceReceipt> arInvoiceReceiptRepo)
            {
            _arInvoiceReceipt = arInvoiceReceiptRepo;
            }

        // GET: api/<ARInvoiceReceiptController>
        [HttpGet]
            public IEnumerable<ArInvoiceReceipt> Get()
            {
                return _arInvoiceReceipt.GetAll();
            }

        // GET api/<ARInvoiceReceiptController>/5
        [HttpGet("{invoiceNbr}")]
            public ArInvoiceReceipt? Get(string invoiceNbr)
            {
                return _arInvoiceReceipt.Get(x => x.InvoiceNbr == invoiceNbr);
            }

        // POST api/<ARInvoiceReceiptController>
        [HttpPost]
            public ArInvoiceReceipt Post([FromBody] ArInvoiceReceipt value)
            {
/*                try
                {
                    var lastSeqNbr = _arInvoiceReceipt.GetList(x => x.in.Value.Date == DateTime.Today)
                        .OrderByDescending(x => x.SeqNbr).FirstOrDefault()?.SeqNbr;

                    if (lastSeqNbr is not null)
                        value.SeqNbr = lastSeqNbr + 1;
                    else
                        value.SeqNbr = 1;
                }
                catch
                {
                    value.SeqNbr = 1;
                }*/
            _arInvoiceReceipt.Add(value);
                return value;
            }

        // PUT api/<ARInvoiceReceiptController>/5
        [HttpPut]
            public void Put([FromBody] ArInvoiceReceipt value)
            {
            _arInvoiceReceipt.Update(value);
            }

        // DELETE api/<ARInvoiceReceiptController>/5
        [HttpDelete("{invoiceNbr}")]
            public void Delete(string invoiceNbr)
            {

                var arInvoiceReceipt = _arInvoiceReceipt.Get(x => x.InvoiceNbr == invoiceNbr);

                if (arInvoiceReceipt is not null)
                    _arInvoiceReceipt.Remove(arInvoiceReceipt);


            }
        }
    

}
