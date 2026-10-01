using DataAccess.Services;
using InvEntry.Contracts.Gst;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[ApiController]
[Route("api/gstr1/historical-migration/stage")]
[Authorize(Roles = "admin")]
public sealed class HistoricalMigrationStageController : ControllerBase
{
    private readonly IHistoricalMigrationStageService _service;
    public HistoricalMigrationStageController(IHistoricalMigrationStageService service) => _service = service;

    [HttpPost]
    public async Task<ActionResult<HistoricalMigrationStageResponse>> Post(
        [FromBody] HistoricalMigrationStageRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.StageAsync(request.PreviewToken, AdministratorIdentity(), cancellationToken)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private string AdministratorIdentity() => User.FindFirst("id")?.Value ?? User.Identity?.Name ?? "admin";
}
