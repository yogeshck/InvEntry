using DataAccess.Services.Advances;
using DataAccess.Services.Schemes;
using InvEntry.Contracts.Schemes;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

/// <summary>
/// One-time import of the shop's RD register (scheme master + payment ledger).
/// Apply = false reports what would be imported; Apply = true imports the
/// schemes without errors. Running it again skips RDs already imported.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class SchemeImportController : ControllerBase
{
    private readonly ISchemeImportService _schemeImportService;

    public SchemeImportController(ISchemeImportService schemeImportService)
    {
        _schemeImportService = schemeImportService;
    }

    // POST: api/schemeimport
    [HttpPost]
    public async Task<ActionResult<SchemeImportResult>> Import(
        [FromBody] SchemeImportRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { message = "Import request is required." });

        try
        {
            return Ok(await _schemeImportService.ImportAsync(request, cancellationToken));
        }
        catch (AdvanceAccountBusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
