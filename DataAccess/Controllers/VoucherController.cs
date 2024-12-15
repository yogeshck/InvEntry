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
        private string DocumentPrefixFormat = ""; //"B{0}";

        private readonly IRepositoryBase<Voucher> _voucher;
        private readonly IRepositoryBase<VoucherType> _voucherTypeRepo;

        public VoucherController(   IRepositoryBase<Voucher> voucherRepo,
                                    IRepositoryBase<VoucherType> voucherTypeRepo )
        {
            _voucher = voucherRepo;
            _voucherTypeRepo = voucherTypeRepo;
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
            if (criteria.BookType is not null)
            {
                return _voucher.GetList(x => x.TransDate.HasValue && x.TransDate.Value.Date >= criteria.From.Date &&
                                                        x.TransDate.Value.Date <= criteria.To.Date
                                                        && x.Mode == criteria.BookType).OrderBy(x => x.VoucherDate);
            } else
            {
                return _voucher.GetList(x => x.TransDate.HasValue && x.TransDate.Value.Date >= criteria.From.Date &&
                                                        x.TransDate.Value.Date <= criteria.To.Date).OrderBy(x => x.VoucherDate);
            }

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

            var voucherType = _voucherTypeRepo.Get(x => x.DocumentType == value.VoucherType);

            voucherType.LastUsedNumber++;

            _voucherTypeRepo.Update(voucherType);

            DocumentPrefixFormat = voucherType.DocNbrPrefix;

            value.VoucherNbr = string.Format("{0}{1}",  DocumentPrefixFormat, 
                                                        voucherType?.LastUsedNumber?.ToString("D4"));

            _voucher.Add(value);
            return value;
        }

        // PUT api/<VoucherController>/5
        [HttpPut]
        public IActionResult Put([FromBody] Voucher value)
        {
            _voucher.Update(value);
            return Ok(value);
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
