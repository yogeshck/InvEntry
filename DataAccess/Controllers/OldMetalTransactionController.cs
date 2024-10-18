using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OldMetalTransactionController : ControllerBase
    {

        private readonly IRepositoryBase<OldMetalTransaction> _oldMetalTransaction;

        public OldMetalTransactionController(IRepositoryBase<OldMetalTransaction> oldMetalTransactionRepo)
        {
            _oldMetalTransaction = oldMetalTransactionRepo;
        }

        // GET: api/<OldMetalTransactionController>
        [HttpGet]
        public IEnumerable<OldMetalTransaction> Get()
        {
            return _oldMetalTransaction.GetAll();
        }

        // GET api/<OldMetalTransactionController>/5
        [HttpGet("{transNbr}")]
        public OldMetalTransaction? Get(string transNbr)
        {
            return _oldMetalTransaction.Get(x => x.TransNbr == transNbr);
        }

        // POST api/<OldMetalTransactionController>
        [HttpPost]
        public OldMetalTransaction Post([FromBody] OldMetalTransaction value)
        {
            _oldMetalTransaction.Add(value);
            return value;
        }

        // PUT api/<OldMetalTransactionController>/5
        [HttpPut]
        public void Put([FromBody] OldMetalTransaction value)
        {
            _oldMetalTransaction.Update(value);
        }

        // DELETE api/<OldMetalTransactionController>/5
        [HttpDelete("{transNbr}")]
        public void Delete(string transNbr)
        {

            var oldMetalTransaction = _oldMetalTransaction.Get(x => x.TransNbr == transNbr);

            if (oldMetalTransaction is not null)
                _oldMetalTransaction.Remove(oldMetalTransaction);


        }
    }


}

