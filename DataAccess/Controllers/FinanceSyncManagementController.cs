using DataAccess.Security;
using DataAccess.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[ApiController]
[Route("api/finance-sync-management")]
[Authorize(AuthenticationSchemes = ManagementServiceAuthenticationDefaults.Scheme)]
public sealed class FinanceSyncManagementController : ControllerBase
{
    private readonly FinanceSyncManagementService _service;
    private readonly FinanceSyncDispatchJobQueue _jobs;
    private readonly ILogger<FinanceSyncManagementController> _logger;
    public FinanceSyncManagementController(FinanceSyncManagementService service,
        FinanceSyncDispatchJobQueue jobs,
        ILogger<FinanceSyncManagementController> logger) =>
        (_service, _jobs, _logger) = (service, jobs, logger);

    [HttpGet("events")]
    public async Task<IActionResult> Events([FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate, [FromQuery] string? status,
        [FromQuery] string? documentNumber, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, [FromQuery] int orgGkey = 0,
        [FromQuery] int locationGkey = 0, CancellationToken token = default)
    {
        if (!ScopeMatches(orgGkey, locationGkey)) return Forbid();
        try { return Ok(await _service.ListAsync(new(fromDate, toDate, status,
            documentNumber, page, pageSize), token)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("events/{id:long}")]
    public async Task<IActionResult> Event(long id, [FromQuery] int orgGkey,
        [FromQuery] int locationGkey, CancellationToken token)
    {
        if (!ScopeMatches(orgGkey, locationGkey)) return Forbid();
        var result = await _service.FindAsync(id, token);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate, [FromQuery] int orgGkey,
        [FromQuery] int locationGkey, CancellationToken token)
    {
        if (!ScopeMatches(orgGkey, locationGkey)) return Forbid();
        try { return Ok(await _service.SummaryAsync(fromDate, toDate, token)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("dispatch")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> Dispatch([FromBody] FinanceSyncDispatchRequest request,
        CancellationToken token)
    {
        if (!ScopeMatches(request.OrgGkey, request.LocationGkey)) return Forbid();
        var actor = CleanActor(Request.Headers["X-FinanceTracker-Actor"].ToString());
        if (string.IsNullOrWhiteSpace(actor) || actor != CleanActor(request.Actor))
            return BadRequest(new { message = "A verified operator identity is required." });
        try
        {
            var eligible = await _service.EligibleIdsAsync(request.EventIds, token);
            if (eligible.Count != request.EventIds.Distinct().Count())
                return Conflict(new { message = "One or more events are not eligible for manual dispatch. Refresh before trying again." });
            var accepted = _jobs.Enqueue(eligible, actor);
            _logger.LogInformation("Finance sync dispatch {JobId} accepted from {Actor}; scope {Org}/{Location}; events {EventIds}",
                accepted.JobId, actor, request.OrgGkey, request.LocationGkey, eligible);
            return AcceptedAtAction(nameof(DispatchStatus), new { jobId = accepted.JobId,
                orgGkey = request.OrgGkey, locationGkey = request.LocationGkey }, accepted);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("dispatch/{jobId:guid}")]
    public IActionResult DispatchStatus(Guid jobId, [FromQuery] int orgGkey,
        [FromQuery] int locationGkey)
    {
        if (!ScopeMatches(orgGkey, locationGkey)) return Forbid();
        var job = _jobs.Find(jobId);
        return job == null ? NotFound() : Ok(job);
    }

    private bool ScopeMatches(int org, int location) =>
        org > 0 && location > 0 && _service.Scope == (org, location);
    private static string CleanActor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        value = value.Trim();
        return value.Length <= 100 ? value : value[..100];
    }
}
