using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinDayBookController : ControllerBase
    {

        private readonly IRepositoryBase<FinDayBook> _finDayBook;

        public FinDayBookController(IRepositoryBase<FinDayBook> finDayBookRepo)
        {
            _finDayBook = finDayBookRepo;
        }

        // GET: api/<FinDayBookController>
        [HttpGet]
        public IEnumerable<FinDayBook> Get()
        {
            return _finDayBook.GetAll();
        }

        // GET api/<FinDayBookController>/5
        [HttpGet("{voucherNbr}")]
        public FinDayBook? Get(string voucherNbr)
        {
            return _finDayBook.Get(x => x.VoucherNbr==voucherNbr);
        }

        // POST api/<FinDayBookController>
        [HttpPost]
        public FinDayBook Post([FromBody] FinDayBook value)
        {
            try
            {
                var lastSeqNbr = _finDayBook.GetList(x => x.VoucherDate.Value.Date == DateTime.Today)
                    .OrderByDescending(x => x.SeqNbr).FirstOrDefault()?.SeqNbr;

                if (lastSeqNbr is not null)
                    value.SeqNbr = lastSeqNbr++;
                else
                    value.SeqNbr = 1;
            }
            catch
            {
                value.SeqNbr = 1;
            }
            _finDayBook.Add(value);
            return value;
        }

        // PUT api/<FinDayBookController>/5
        [HttpPut]
        public void Put([FromBody] FinDayBook value)
        {
            _finDayBook.Update(value);
        }

        // DELETE api/<FinDayBookController>/5
        [HttpDelete("{voucherNbr}")]
        public void Delete(string voucherNbr)
        {

            var finDayBook = _finDayBook.Get(x => x.VoucherNbr == voucherNbr);

            if (finDayBook is not null)
                _finDayBook.Remove(finDayBook);


        }
    }
}
