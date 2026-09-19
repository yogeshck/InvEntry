using DataAccess.Services;
using InvEntry.Contracts.Gst;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/gstr1")]
[ApiController]
public sealed class Gstr1Controller : ControllerBase
{
    private readonly IGstr1ReportQueryService _service;

    public Gstr1Controller(IGstr1ReportQueryService service)
    {
        _service = service;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Gstr1ReturnSummaryResponse>> GetSummary(
        [FromQuery] Gstr1ReturnQuery query, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetSummaryAsync(
                query.SupplierGstin, query.ReturnPeriod, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("documents")]
    public async Task<ActionResult<IReadOnlyList<Gstr1DocumentResponse>>> GetDocuments(
        [FromQuery] Gstr1ReturnQuery query, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetDocumentsAsync(
                query.SupplierGstin, query.ReturnPeriod, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("documents/{gkey:long}/lines")]
    public async Task<ActionResult<IReadOnlyList<Gstr1DocumentLineResponse>>> GetDocumentLines(
        long gkey, [FromQuery] Gstr1ReturnQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var lines = await _service.GetDocumentLinesAsync(
                gkey, query.SupplierGstin, query.ReturnPeriod, cancellationToken);
            return lines is null ? NotFound() : Ok(lines);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
