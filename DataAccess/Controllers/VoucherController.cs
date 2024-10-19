using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {

        private readonly IRepositoryBase<Voucher> _voucher;

        public VoucherController(IRepositoryBase<Voucher> voucherRepo)
        {
            _voucher = voucherRepo;
        }

        // GET: api/<VoucherController>
        [HttpGet]
        public IEnumerable<Voucher> Get()
        {
            return _voucher.GetAll();
        }

        // GET: api/<InvoiceController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<Voucher> FilterVoucher([FromBody] VoucherSearchOption criteria)
        {
            return _voucher.GetList(x => x.TransDate.HasValue && x.TransDate.Value.Date >= criteria.From.Date &&
                                                        x.TransDate.Value.Date <= criteria.To.Date);
        }


        // GET api/<VoucherController>/5
        [HttpGet("{voucherNbr}")]
        public Voucher? Get(string voucherNbr)
        {
            return _voucher.Get(x => x.VoucherNbr==voucherNbr);
        }

        // POST api/<VoucherController>
        [HttpPost]
        public Voucher Post([FromBody] Voucher value)
        {
            try
            {
                var lastSeqNbr = _voucher.GetList(x => x.VoucherDate.Value.Date == DateTime.Today)
                    .OrderByDescending(x => x.SeqNbr).FirstOrDefault()?.SeqNbr;

                if (lastSeqNbr is not null)
                    value.SeqNbr = lastSeqNbr + 1;
                else
                    value.SeqNbr = 1;
            }
            catch
            {
                value.SeqNbr = 1;
            }
            _voucher.Add(value);
            return value;
        }

        // PUT api/<VoucherController>/5
        [HttpPut]
        public void Put([FromBody] Voucher value)
        {
            _voucher.Update(value);
        }

        // DELETE api/<VoucherController>/5
        [HttpDelete("{voucherNbr}")]
        public void Delete(string voucherNbr)
        {

            var finDayBook = _voucher.Get(x => x.VoucherNbr == voucherNbr);

            if (finDayBook is not null)
                _voucher.Remove(finDayBook);


        }
    }
}
