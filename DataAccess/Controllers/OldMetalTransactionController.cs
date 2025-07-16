using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OldMetalTransactionController : ControllerBase
    {
        private string DocumentPrefixFormat = "";  

        private readonly IRepositoryBase<OldMetalTransaction> _oldMetalTransaction;
        private readonly IRepositoryBase<VoucherType> _voucherTypeRepo;

        public OldMetalTransactionController(   IRepositoryBase<OldMetalTransaction> oldMetalTransactionRepo,
                                                IRepositoryBase<VoucherType> voucherTypeRepo)
        {
            _oldMetalTransaction = oldMetalTransactionRepo;
            _voucherTypeRepo = voucherTypeRepo;
        }

        // GET: api/<OldMetalTransactionController>
        [HttpGet]
        public IEnumerable<OldMetalTransaction> Get()
        {
            return _oldMetalTransaction.GetAll();
        }

        // GET: api/<OldMetalTransactionController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<OldMetalTransaction> FilterTrans([FromBody] DateSearchOption criteria)
        {
            return _oldMetalTransaction.GetList(x => x.TransDate.HasValue && x.TransDate.Value.Date >= criteria.From.Date &&
                                                        x.TransDate.Value.Date <= criteria.To.Date);
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

            var docType = _voucherTypeRepo.Get(x => x.DocumentType == value.TransType);

            docType.LastUsedNumber++;

            _voucherTypeRepo.Update(docType);

            DocumentPrefixFormat = docType.DocNbrPrefix;

            value.TransNbr = string.Format("{0}{1}", DocumentPrefixFormat,
                                                        docType?.LastUsedNumber?.ToString("D4"));

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

