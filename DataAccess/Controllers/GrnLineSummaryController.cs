using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrnLineSummaryController : ControllerBase
    {

        private IRepositoryBase<GrnLineSummary> _grnLineSumryRepo;

        public GrnLineSummaryController(IRepositoryBase<GrnLineSummary> grnLineSummaryRepo)
        {
            _grnLineSumryRepo = grnLineSummaryRepo;
        }

        // GET: api/<GrnLineSummaryController>
        [HttpGet]
        public IEnumerable<GrnLineSummary> GetSummaryLines()
        {
            return _grnLineSumryRepo.GetAll();
        }

        // GET api/<GrnLineSummaryController>/5
        [HttpGet("{grnHdrGkey}")]
        public async Task<IActionResult> GetByHdrGKey(int grnHdrGkey)
        {
            return Ok(_grnLineSumryRepo.GetList(x => x.GrnHdrGkey == grnHdrGkey));

        }

        // POST api/<GrnLineSummaryController>
        [HttpPost]
        public GrnLineSummary Post([FromBody] GrnLineSummary value)
        {
            _grnLineSumryRepo.Add(value);
            return value;
        }

    }
}
