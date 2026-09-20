using DataAccess.Services;
using InvEntry.Contracts.Gst;
using InvEntry.Contracts.Gst.Export;
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
    private readonly IGstr1HsnSummaryService _hsnSummaryService;
    private readonly IGstr1DocumentsIssuedService _documentsIssuedService;
    private readonly IGstr1B2csSummaryService _b2csSummaryService;
    private readonly IGstr1ExportPreparationService _exportPreparationService;
    private readonly IGstr1JsonExportService _jsonExportService;

    public Gstr1Controller(
        IGstr1ReportQueryService service,
        IGstr1ValidationService validationService,
        IGstr1StagingEnrichmentService enrichmentService,
        IGstr1HsnSummaryService hsnSummaryService,
        IGstr1BackfillService backfillService,
        IGstr1DocumentsIssuedService documentsIssuedService,
        IGstr1B2csSummaryService b2csSummaryService,
        IGstr1ExportPreparationService exportPreparationService,
        IGstr1JsonExportService jsonExportService)
    {
        _service = service;
        _validationService = validationService;
        _backfillService = backfillService;
        _enrichmentService = enrichmentService;
        _hsnSummaryService = hsnSummaryService;
        _documentsIssuedService = documentsIssuedService;
        _b2csSummaryService = b2csSummaryService;
        _exportPreparationService = exportPreparationService;
        _jsonExportService = jsonExportService;

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

    [HttpGet("hsn-summary")]
    public async Task<ActionResult<Gstr1HsnSummaryResponse>> GetHsnSummary(
    [FromQuery] string supplierGstin,
    [FromQuery] string returnPeriod,
    CancellationToken cancellationToken)
    {
        try
        {
            var result = await _hsnSummaryService.GetSummaryAsync(
                new Gstr1HsnSummaryQuery
                {
                    SupplierGstin = supplierGstin,
                    ReturnPeriod = returnPeriod
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("documents-issued")]
    public async Task<ActionResult<Gstr1DocumentsIssuedResponse>> GetDocumentsIssued(
        [FromQuery] Gstr1DocumentsIssuedQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _documentsIssuedService.GetAsync(
                query.SupplierGstin,
                query.ReturnPeriod,
                cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("b2cs-summary")]
    public async Task<ActionResult<Gstr1B2csSummaryResponse>> GetB2csSummary(
        [FromQuery] Gstr1B2csSummaryQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _b2csSummaryService.GetSummaryAsync(query, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("export-preparation")]
    public async Task<ActionResult<Gstr1ExportPreparationResponse>> GetExportPreparation(
        [FromQuery] string supplierGstin,
        [FromQuery] string returnPeriod,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _exportPreparationService.PrepareAsync(
                supplierGstin, returnPeriod, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    [HttpGet("export-json")]
    [Produces("application/json")]
    public async Task<ActionResult<Gstr1GstnExportResponse>> GetExportJson(
        [FromQuery] string supplierGstin,
        [FromQuery] string returnPeriod,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _jsonExportService.ExportAsync(
                supplierGstin, returnPeriod, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                error = ex.Message,
                supplierGstin,
                returnPeriod,
                validationEndpoint = "/api/gstr1/validation"
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
