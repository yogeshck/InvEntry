using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
        [Route("api/[controller]")]
        [ApiController]
        public class EstimateLineController : ControllerBase
        {
            private IRepositoryBase<EstimateLine> _estimateLineRepository;

            public EstimateLineController(IRepositoryBase<EstimateLine> estimateLineRepository)
            {
                _estimateLineRepository = estimateLineRepository;
            }



            // GET: api/<InvoiceController>
            [HttpGet]
            public IEnumerable<EstimateLine> GetHeader()
            {
                return _estimateLineRepository.GetAll();
            }

            // GET api/<InvoiceController>/5
            [HttpGet("{estNbr}")]
            public EstimateLine? Get(long estNbr)
            {
                return _estimateLineRepository.Get(x => x.EstLineNbr == estNbr);
            }

            // POST api/<InvoiceController>
            [HttpPost]
            public EstimateLine Post([FromBody] EstimateLine value)
            {
                _estimateLineRepository.Add(value);
                return value;
            }

            // PUT api/<InvoiceController>/5
            [HttpPut("{estNbr}")]
            public void Put(int estNbr, [FromBody] EstimateLine value)
            {
                value.EstLineNbr = estNbr;
                _estimateLineRepository.Update(value);
            }

            // DELETE api/<InvoiceController>/5
            [HttpDelete("{estNbr}")]
            public void Delete(long estNbr)
            {
                _estimateLineRepository.Remove(Get(estNbr));
            }
        }
    }
