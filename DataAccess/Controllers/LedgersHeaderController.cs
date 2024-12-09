using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class LedgersHeaderController : ControllerBase
    {
        private readonly IRepositoryBase<LedgersHeader> _ledgersHeaderRepository;

        public LedgersHeaderController(IRepositoryBase<LedgersHeader> ledgersHeaderRepository)
        {
            _ledgersHeaderRepository = ledgersHeaderRepository;

        }

        // GET: api/<MtblLedgerController>
        [HttpGet]
        public IEnumerable<LedgersHeader> Get()
        {
            return _ledgersHeaderRepository.GetAll();
        }

        // GET api/<InvoiceController>/5
        [HttpGet("{ledgerGkey}/{custGkey}")]
        public LedgersHeader? Get(int ledgerGkey, int custGkey)
        {
            return _ledgersHeaderRepository.Get(x => x.CustGkey == custGkey && x.MtblLedgersGkey == ledgerGkey);
        }

        // POST api/<MtblLedgerController>
        [HttpPost]
        public LedgersHeader Post([FromBody] LedgersHeader value)
        {
            _ledgersHeaderRepository.Add(value);
            return value;
        }

        // PUT api/<MtblLedgerController>/5
        [HttpPut("{gkey}")]
        public void Put(int gkey, [FromBody] LedgersHeader value)
        {
            value.Gkey = gkey;
            _ledgersHeaderRepository.Update(value);
        }

    }
}
