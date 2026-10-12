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
    private readonly FinanceClosingEligibilityService _closingEligibility;
    private readonly FinanceClosingFenceService _closingFence;
    private readonly FinanceSyncDispatchJobQueue _jobs;
    private readonly ILogger<FinanceSyncManagementController> _logger;
    public FinanceSyncManagementController(FinanceSyncManagementService service,
        FinanceClosingEligibilityService closingEligibility,
        FinanceClosingFenceService closingFence,
        FinanceSyncDispatchJobQueue jobs,
        ILogger<FinanceSyncManagementController> logger) =>
        (_service, _closingEligibility, _closingFence, _jobs, _logger) =
            (service, closingEligibility, closingFence, jobs, logger);

    [HttpGet("closing-eligibility")]
    public async Task<IActionResult> ClosingEligibility([FromQuery] int orgGkey,
        [FromQuery] int locationGkey, [FromQuery] DateOnly businessDate,
        CancellationToken token)
    {
        if (!ScopeMatches(orgGkey, locationGkey)) return Forbid();
        try
        {
            return Ok(await _closingEligibility.EvaluateAsync(
                orgGkey, locationGkey, businessDate, token));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("closing-reservations")]
    public async Task<IActionResult> ReserveClosing(
        [FromBody] FinanceClosingReservationRequest request, CancellationToken token)
    {
        if (!ScopeMatches(request.OrgGkey, request.LocationGkey)) return Forbid();
        if (request.ReservationId == Guid.Empty)
            return BadRequest(new { message = "A reservation identifier is required." });
        try { return Ok(await _closingFence.ReserveAsync(request, token)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("closing-reservations/{reservationId:guid}/confirm")]
    public Task<IActionResult> ConfirmClosing(Guid reservationId, [FromQuery] int orgGkey,
        [FromQuery] int locationGkey, CancellationToken token) => ScopeMatches(orgGkey, locationGkey)
        ? Transition(reservationId, _closingFence.ConfirmAsync, token)
        : Task.FromResult<IActionResult>(Forbid());

    [HttpPost("closing-reservations/{reservationId:guid}/cancel")]
    public Task<IActionResult> CancelClosing(Guid reservationId, [FromQuery] int orgGkey,
        [FromQuery] int locationGkey, CancellationToken token) => ScopeMatches(orgGkey, locationGkey)
        ? Transition(reservationId, _closingFence.CancelAsync, token)
        : Task.FromResult<IActionResult>(Forbid());

    [HttpPost("closing-reservations/{reservationId:guid}/uncertain")]
    public Task<IActionResult> MarkClosingUncertain(Guid reservationId, [FromQuery] int orgGkey,
        [FromQuery] int locationGkey, CancellationToken token) => ScopeMatches(orgGkey, locationGkey)
        ? Transition(reservationId, _closingFence.MarkUncertainAsync, token)
        : Task.FromResult<IActionResult>(Forbid());

    [HttpPost("closing-reservations/{reservationId:guid}/cancel-verified-open")]
    public Task<IActionResult> CancelClosingAfterVerifiedOpen(Guid reservationId,
        [FromQuery] int orgGkey, [FromQuery] int locationGkey, CancellationToken token) =>
        ScopeMatches(orgGkey, locationGkey)
        ? Transition(reservationId, _closingFence.CancelAfterVerifiedOpenAsync, token)
        : Task.FromResult<IActionResult>(Forbid());

    private async Task<IActionResult> Transition(Guid id,
        Func<Guid, CancellationToken, Task> action, CancellationToken token)
    {
        try { await action(id, token); return Ok(new { reservationId = id }); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

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
