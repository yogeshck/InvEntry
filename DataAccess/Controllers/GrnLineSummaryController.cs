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
        private readonly IUnitOfWork _unitOfWork;

        public GrnLineSummaryController(IRepositoryBase<GrnLineSummary> grnLineSummaryRepo,
                            IUnitOfWork unitOfWork)
        {
                _grnLineSumryRepo = grnLineSummaryRepo;
                _unitOfWork = unitOfWork;
        }

        // GET: api/<GrnLineSummaryController>
        [HttpGet]
        public IEnumerable<GrnLineSummary> GetSummaryLines()
        {
            return _grnLineSumryRepo.GetAll();
        }

        [HttpGet("{grnHdrGkey}")]
        public IActionResult GetByHdrGKey(int grnHdrGkey)
        {
            return Ok(
                _grnLineSumryRepo.GetList(
                    x => x.GrnHdrGkey == grnHdrGkey));
        }

        [HttpPost]
        public async Task<ActionResult<GrnLineSummary>> Post(
            [FromBody] GrnLineSummary value)
        {
            _grnLineSumryRepo.Add(value);

            await _unitOfWork.SaveChangesAsync();

            return Ok(value);
        }

    }
}
