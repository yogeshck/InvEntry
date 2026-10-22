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
        private readonly IUnitOfWork _unitOfWork;

        public LedgersHeaderController(IRepositoryBase<LedgersHeader> ledgersHeaderRepository,
                                       IUnitOfWork unitOfWork)
        {
            _ledgersHeaderRepository = ledgersHeaderRepository;
            _unitOfWork = unitOfWork;

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
        public async Task<IActionResult> Post([FromBody] LedgersHeader value)
        {
            _ledgersHeaderRepository.Add(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

        // PUT api/<MtblLedgerController>/5
        [HttpPut("{gkey}")]
        public async Task<IActionResult> Put(int gkey, [FromBody] LedgersHeader value)
        {
            value.Gkey = gkey;
            _ledgersHeaderRepository.Update(value);
            await _unitOfWork.SaveChangesAsync();

            return Ok(value);

        }

    }
}
