using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Controllers;

[ApiController]
[Route("api/finance-sync/reconciliation")]
[Authorize(Roles = "admin")]
public sealed class FinanceSyncReconciliationController : ControllerBase
{
    private readonly FinanceSyncReconciliationService _service;

    public FinanceSyncReconciliationController(
        FinanceSyncReconciliationService service) => _service = service;

    [HttpPost("dry-run")]
    public async Task<ActionResult<FinanceSyncReconciliationReport>> DryRun(
        [FromBody] FinanceSyncDryRunRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.DryRunAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("recover")]
    public async Task<ActionResult<FinanceSyncRecoveryResult>> Recover(
        [FromBody] FinanceSyncRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.RecoverAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "The recovery was not applied because the event was created concurrently. Run the dry-run again."
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
