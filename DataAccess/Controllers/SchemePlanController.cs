using DataAccess.Services.Advances;
using DataAccess.Services.Schemes;
using InvEntry.Contracts.Schemes;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

/// <summary>
/// Savings scheme (RD) plans. Editing a plan affects only new enrollments;
/// existing ones keep the terms snapshotted when they enrolled.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class SchemePlanController : ControllerBase
{
    private readonly ISchemePlanService _schemePlanService;

    public SchemePlanController(ISchemePlanService schemePlanService)
    {
        _schemePlanService = schemePlanService;
    }

    // GET: api/schemeplan?activeOnly=true
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchemePlanDto>>> Get(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        return Ok(await _schemePlanService.GetPlansAsync(activeOnly, cancellationToken));
    }

    // GET: api/schemeplan/5
    [HttpGet("{planGkey:int}")]
    public async Task<ActionResult<SchemePlanDto>> Get(
        int planGkey,
        CancellationToken cancellationToken)
    {
        var plan = await _schemePlanService.GetPlanAsync(planGkey, cancellationToken);
        return plan is null ? NotFound() : Ok(plan);
    }

    // GET: api/schemeplan/metal-options
    [HttpGet("metal-options")]
    public async Task<ActionResult<IEnumerable<SchemeMetalOption>>> GetMetalOptions(
        CancellationToken cancellationToken)
    {
        return Ok(await _schemePlanService.GetMetalOptionsAsync(cancellationToken));
    }

    // POST: api/schemeplan  (Gkey 0 creates, otherwise updates)
    [HttpPost]
    public async Task<ActionResult<SchemePlanDto>> Save(
        [FromBody] SchemePlanDto plan,
        CancellationToken cancellationToken)
    {
        if (plan is null)
            return BadRequest(new { message = "Scheme plan is required." });

        try
        {
            return Ok(await _schemePlanService.SavePlanAsync(plan, cancellationToken));
        }
        catch (AdvanceAccountBusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
