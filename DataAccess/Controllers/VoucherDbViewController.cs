using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;


namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherDbViewController : ControllerBase
    {
        private readonly IRepositoryBase<VoucherDbView> _voucherDbViewRepo;
        private readonly ILogger<VoucherDbViewController> _logger;

        public VoucherDbViewController(IRepositoryBase<VoucherDbView> voucherDbViewRepo,
                                        ILogger<VoucherDbViewController> logger)
        {
            _voucherDbViewRepo = voucherDbViewRepo;
            _logger = logger;
        }

        // GET: api/<GrnDbViewController>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("All Voucher View");
            return Ok(_voucherDbViewRepo.GetAll());
        }

        // GET: api/<VoucherDbViewController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<VoucherDbView> FilterHeader([FromBody] DateSearchOption criteria)
        {
            if (criteria.Filter1 is not null)
            {
                return _voucherDbViewRepo.GetList(x => x.TransDate.HasValue && x.TransDate.Value.Date >= criteria.From.Date &&
                                                        x.TransDate.Value.Date <= criteria.To.Date
                                                        && x.Mode == criteria.Filter1).OrderBy(x => x.VoucherDate);
    } else
            {
                return _voucherDbViewRepo.GetList(x => x.TransDate.HasValue && x.TransDate.Value.Date >= criteria.From.Date &&
                                                        x.TransDate.Value.Date <= criteria.To.Date).OrderBy(x => x.VoucherDate);
}

        }

    }


}