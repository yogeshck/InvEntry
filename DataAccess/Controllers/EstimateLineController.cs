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
            private readonly IUnitOfWork _unitOfWork;

            public EstimateLineController(IRepositoryBase<EstimateLine> estimateLineRepository,
                                          IUnitOfWork unitOfWork)
            {
                _estimateLineRepository = estimateLineRepository;
                _unitOfWork = unitOfWork;
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
            public async Task<EstimateLine> Post([FromBody] EstimateLine value)
            {
                _estimateLineRepository.Add(value);
                await _unitOfWork.SaveChangesAsync();
                return value;
            }

            // PUT api/<InvoiceController>/5
            [HttpPut("{estNbr}")]
            public async Task Put(int estNbr, [FromBody] EstimateLine value)
            {
                value.EstLineNbr = estNbr;
                _estimateLineRepository.Update(value);
                await _unitOfWork.SaveChangesAsync();
            }

            // DELETE api/<InvoiceController>/5
            [HttpDelete("{estNbr}")]
            public async Task Delete(long estNbr)
            {
                var line = Get(estNbr);

                if (line is not null)
                {
                    _estimateLineRepository.Remove(line);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }
    }
