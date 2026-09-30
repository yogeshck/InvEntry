using DataAccess.Services;
using InvEntry.Contracts.Gst;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[ApiController]
[Route("api/gstr1/historical-invoice-audit")]
[Authorize(Roles = "admin")]
public sealed class HistoricalInvoiceAuditController : ControllerBase
{
    private readonly IHistoricalInvoiceAuditService _service;

    public HistoricalInvoiceAuditController(IHistoricalInvoiceAuditService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<HistoricalInvoiceAuditResponse>> Get(
        [FromQuery] string supplierGstin,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetAsync(
                supplierGstin,
                fromDate,
                toDate,
                cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
