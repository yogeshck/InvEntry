using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DailyRateController : BaseController<DailyRate>
    {
        public DailyRateController(
            IRepositoryBase<DailyRate> repository,
            IUnitOfWork unitOfWork)
            : base(repository, unitOfWork)
        {
        }


        // =========================================================
        // GET: api/dailyrate/latest
        // =========================================================

        [HttpGet("latest")]
        public IEnumerable<DailyRate> GetLatest()
        {
            return _repository.GetList(
                x =>
                    x.EffectiveDate.Date >=
                    DateTime.Now.Date.AddDays(-1)
                    && x.IsDisplay);
        }


        // =========================================================
        // POST: api/dailyrate/save
        // =========================================================

        [HttpPost("save")]
        public async Task<ActionResult<IEnumerable<DailyRate>>> PostList(
            [FromBody] IEnumerable<DailyRate> data)
        {
            if (data is null)
                return BadRequest();

            var rates = data.ToList();

            if (rates.Count == 0)
                return Ok(rates);

            _repository.AddRange(rates);

            await _unitOfWork.SaveChangesAsync();

            return Ok(rates);
        }


        // =========================================================
        // PUT: api/dailyrate/{id}
        // =========================================================

        [HttpPut("{id}")]
        public async Task<ActionResult<DailyRate>> Put(
            long id,
            [FromBody] DailyRate data)
        {
            if (data is null)
                return BadRequest();

            _repository.Update(data);

            await _unitOfWork.SaveChangesAsync();

            return Ok(data);
        }


        // =========================================================
        // PUT: api/dailyrate/update
        // =========================================================

        [HttpPut("update")]
        public async Task<ActionResult<IEnumerable<DailyRate>>> Put(
            [FromBody] IEnumerable<DailyRate> data)
        {
            if (data is null)
                return BadRequest();

            var rates = data.ToList();

            if (rates.Count == 0)
                return Ok(rates);

            _repository.BulkUpdate(rates);

            await _unitOfWork.SaveChangesAsync();

            return Ok(rates);
        }
    }
}