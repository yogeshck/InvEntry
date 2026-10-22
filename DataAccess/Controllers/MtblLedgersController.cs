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
        private readonly IUnitOfWork _unitOfWork;

        public MtblLedgersController(IRepositoryBase<MtblLedger> mtblLedgerRepository,
                                     IUnitOfWork unitOfWork)
        {
            _mtblLedgerRepository = mtblLedgerRepository;
            _unitOfWork = unitOfWork;

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

        // GET: api/<MtblLedgerController>
        [HttpGet("accountGroup/{accGroupName}")]
        public IEnumerable<MtblLedger> Get(string accGroupName)
        {
            return _mtblLedgerRepository.GetList(x => x.AccountGroupName == accGroupName);
        }


        // POST api/<MtblLedgerController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MtblLedger value)
        {
            _mtblLedgerRepository.Add(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

        // PUT api/<MtblLedgerController>/5
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] MtblLedger value)
        {
            _mtblLedgerRepository.Update(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

    }
}
