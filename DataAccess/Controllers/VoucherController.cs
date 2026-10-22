using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Services;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherController : ControllerBase
    {
        private string DocumentPrefixFormat = ""; //"B{0}";

        private readonly IRepositoryBase<Voucher> _voucher;
        private readonly IRepositoryBase<VoucherType> _voucherTypeRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly FinanceSyncService? _financeSyncService;
        private readonly FinanceSyncOptions _financeSyncOptions;
        private readonly ILogger<VoucherController> _logger;

        public VoucherController(   IRepositoryBase<Voucher> voucherRepo,
                                    IRepositoryBase<VoucherType> voucherTypeRepo,
                                    IUnitOfWork unitOfWork,
                                    FinanceSyncService? financeSyncService = null,
                                    IOptions<FinanceSyncOptions>? financeSyncOptions = null,
                                    ILogger<VoucherController>? logger = null )
        {
            _voucher = voucherRepo;
            _voucherTypeRepo = voucherTypeRepo;
            _unitOfWork = unitOfWork;
            _financeSyncService = financeSyncService;
            _financeSyncOptions = financeSyncOptions?.Value ?? new FinanceSyncOptions { Enabled = false };
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<VoucherController>.Instance;
        }

        // GET: api/<VoucherController>
        [HttpGet]
        public IEnumerable<Voucher> Get()
        {
            return _voucher.GetAll();
        }

        // GET: api/<InvoiceController>/24-Sep-2024/25-Sep-2024
        [HttpPost("filter")]
        public IEnumerable<Voucher> FilterVoucher([FromBody] DateSearchOption criteria)
        {
            if (criteria.Filter1 is not null)
            {
                return _voucher.GetList(x => x.TransDate.HasValue && x.TransDate.Value.Date >= criteria.From.Date &&
                                                        x.TransDate.Value.Date <= criteria.To.Date
                                                        && x.Mode == criteria.Filter1).OrderBy(x => x.VoucherDate);
            } else
            {
                return _voucher.GetList(x => x.TransDate.HasValue && x.TransDate.Value.Date >= criteria.From.Date &&
                                                        x.TransDate.Value.Date <= criteria.To.Date).OrderBy(x => x.VoucherDate);
            }

        }


        // GET api/<VoucherController>/5
        [HttpGet("{voucherNbr}")]
        public Voucher? Get(string voucherNbr)
        {
            return _voucher.Get(x => x.VoucherNbr==voucherNbr);
        }

        [HttpGet("refDocNbr/{refDocNbr}")]
        public IEnumerable<Voucher> GetByRefDocNbr(string refDocNbr)
        {
            return _voucher.GetList(
                x => x.RefDocNbr == refDocNbr &&
                     x.VoucherType == "Advance Receipt");
        }

        // POST api/<VoucherController>
        [HttpPost]
        public async Task<ActionResult<Voucher>> Post([FromBody] Voucher value)
        {
            try
            {
                // Date range rather than VoucherDate.Value.Date, so SQL can seek instead of converting every row.
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var lastSeqNbr = _voucher.GetList(x => x.VoucherDate >= today && x.VoucherDate < tomorrow)
                    .OrderByDescending(x => x.SeqNbr).FirstOrDefault()?.SeqNbr;

                if (lastSeqNbr is not null)
                    value.SeqNbr = lastSeqNbr + 1;
                else
                    value.SeqNbr = 1;
            }
            catch
            {
                value.SeqNbr = 1;
            }

            var voucherType = _voucherTypeRepo.Get(x => x.DocumentType == value.VoucherType);

            if (voucherType is null)
                return BadRequest($"Voucher type '{value.VoucherType}' is not configured.");

            voucherType.LastUsedNumber++;

            _voucherTypeRepo.Update(voucherType);

            DocumentPrefixFormat = voucherType.DocNbrPrefix;

            value.VoucherNbr = string.Format("{0}{1}",  DocumentPrefixFormat, 
                                                        voucherType?.LastUsedNumber?.ToString("D4"));

            _voucher.Add(value);

            if (!IsPossibleCollection(value))
            {
                await _unitOfWork.SaveChangesAsync();
                return value;
            }

            // A receipt against an already finalised invoice is money collected later.
            // Its FinanceTracker event is saved with the voucher, as invoice finalisation
            // does, so the collection can never be in InvEntry but missing from finance.
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.SaveChangesAsync();

            try
            {
                await _financeSyncService!.QueueCollectionSyncAsync(value.Gkey,
                    _financeSyncOptions.OrgGkey, _financeSyncOptions.LocationGkey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Finance collection event could not be queued for voucher {VoucherNbr} on {RefDocNbr}; the voucher is not saved.",
                    value.VoucherNbr, value.RefDocNbr);
                throw;
            }

            await transaction.CommitAsync();
            return value;
        }

        private bool IsPossibleCollection(Voucher value) =>
            _financeSyncOptions.Enabled &&
            _financeSyncService is not null &&
            string.Equals(value.TransType?.Trim(), "Receipt", StringComparison.OrdinalIgnoreCase) &&
            value.RefDocGkey is not null &&
            !string.IsNullOrWhiteSpace(value.RefDocNbr);

        // PUT api/<VoucherController>/5
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Voucher value)
        {
            _voucher.Update(value);
            await _unitOfWork.SaveChangesAsync();
            return Ok(value);
        }

        // DELETE api/<VoucherController>/5
        [HttpDelete("{voucherNbr}")]
        public async Task Delete(string voucherNbr)
        {

            var finDayBook = _voucher.Get(x => x.VoucherNbr == voucherNbr);

            if (finDayBook is not null)
            {
                _voucher.Remove(finDayBook);
                await _unitOfWork.SaveChangesAsync();
            }


        }
    }
}
