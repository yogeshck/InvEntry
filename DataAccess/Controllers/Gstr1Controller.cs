using DataAccess.Services;
using InvEntry.Contracts.Gst;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/gstr1")]
[ApiController]
public sealed class Gstr1Controller : ControllerBase
{
    private readonly IGstr1ReportQueryService _service;
    private readonly IGstr1ValidationService _validationService;
    private readonly IGstr1BackfillService _backfillService;
    private readonly IGstr1StagingEnrichmentService _enrichmentService;

    public Gstr1Controller(
        IGstr1ReportQueryService service,
        IGstr1ValidationService validationService,
        IGstr1StagingEnrichmentService enrichmentService,
        IGstr1BackfillService backfillService )
    {
        _service = service;
        _validationService = validationService;
        _backfillService = backfillService;
        _enrichmentService = enrichmentService;

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

    [HttpGet("validation")]
    public async Task<ActionResult<Gstr1ValidationResponse>> Validate(
    [FromQuery] Gstr1ReturnQuery query,
    CancellationToken cancellationToken)
    {
        try
        {
            var result = await _validationService.ValidateAsync(
                query.SupplierGstin,
                query.ReturnPeriod,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("enrich")]
    public async Task<ActionResult<Gstr1EnrichmentResponse>> Enrich(
    [FromBody] Gstr1EnrichmentRequest request,
    CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _enrichmentService.EnrichAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                error = ex.Message
            });
        }
    }

    [HttpPost("backfill")]
    public async Task<ActionResult<Gstr1BackfillResponse>> Backfill(
    [FromBody] Gstr1BackfillRequest request,
    CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _backfillService.BackfillAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
    }

}
