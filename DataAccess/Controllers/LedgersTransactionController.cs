using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LedgersTransactionController : ControllerBase
    {

        private readonly IRepositoryBase<LedgersTransaction> _ledgersTransactionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LedgersTransactionController(IRepositoryBase<LedgersTransaction> ledgersTransactionRepository,
                                            IUnitOfWork unitOfWork)
        {
            _ledgersTransactionRepository = ledgersTransactionRepository;
            _unitOfWork = unitOfWork;

        }

        // GET: api/<MtblLedgerController>
        [HttpGet]
        public IEnumerable<LedgersTransaction> Get()
        {
            return _ledgersTransactionRepository.GetAll();
        }

        // POST api/<MtblLedgerController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LedgersTransaction value)
        {
            _ledgersTransactionRepository.Add(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

        // PUT api/<MtblLedgerController>/5
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] LedgersTransaction value)
        {
            _ledgersTransactionRepository.Update(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }
    }
}
