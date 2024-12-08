using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MtblLedgersController : ControllerBase
    {
        private IRepositoryBase<MtblLedger> _mtblLedgerRepository;

        public MtblLedgersController(IRepositoryBase<MtblLedger> mtblLedgerRepository  )
        {
            _mtblLedgerRepository = mtblLedgerRepository;

        }

        // GET: api/<MtblLedgerController>
        [HttpGet]
        public IEnumerable<MtblLedger> Get()
        {
            return _mtblLedgerRepository.GetAll();
        }

        // GET api/<InvoiceController>/5
        [HttpGet("{lAccountCode}")]
        public MtblLedger? Get(int lAccountCode)
        {
            return _mtblLedgerRepository.Get(x => x.LedgerAccountCode == lAccountCode);
        }

        // POST api/<MtblLedgerController>
        [HttpPost]
        public MtblLedger Post([FromBody] MtblLedger value)
        {
            _mtblLedgerRepository.Add(value);
            return value;
        }

        // PUT api/<MtblLedgerController>/5
        [HttpPut]
        public void Put([FromBody] MtblLedger value)
        {
            _mtblLedgerRepository.Update(value);
        }

    }
}
