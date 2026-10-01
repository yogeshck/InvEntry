using DataAccess.Services;
using InvEntry.Contracts.Gst;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[ApiController]
[Route("api/gstr1/historical-migration/preview")]
[Authorize(Roles = "admin")]
public sealed class HistoricalMigrationPreviewController : ControllerBase
{
    private readonly IHistoricalMigrationPreviewService _service;
    public HistoricalMigrationPreviewController(IHistoricalMigrationPreviewService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<HistoricalMigrationPreviewResponse>> Post(
        [FromBody] HistoricalMigrationPreviewRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.PreviewAsync(request, AdministratorIdentity(), cancellationToken)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private string AdministratorIdentity() => User.FindFirst("id")?.Value ?? User.Identity?.Name ?? "admin";
}
