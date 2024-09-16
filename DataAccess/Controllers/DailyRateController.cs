using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DailyRateController : BaseController<DailyRate>
    {
        public DailyRateController(IRepositoryBase<DailyRate> repository) : base(repository)
        {
        }

        // GET api/<DailyRateController>/latest
        [HttpGet("latest")]
        public IEnumerable<DailyRate> GetLatest()
        {
            return _repository.GetList(x => x.EffectiveDate.Date >= DateTime.Now.Date.AddDays(-1) && x.IsDisplay);
        }

        [HttpPost("save")]
        public void PostList([FromBody] IEnumerable<DailyRate> data) 
        {
            _repository.AddRange(data);
        }
    }
}
