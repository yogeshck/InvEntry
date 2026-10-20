using DataAccess.Services.Advances;
using DataAccess.Services.Schemes;
using InvEntry.Contracts.Schemes;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

/// <summary>
/// Customer enrollments in savings scheme (RD) plans. Installments are paid
/// through api/advanceaccount/receipt with the enrollment's AdvanceAccountGkey.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class SchemeEnrollmentController : ControllerBase
{
    private readonly ISchemeEnrollmentService _schemeEnrollmentService;

    public SchemeEnrollmentController(ISchemeEnrollmentService schemeEnrollmentService)
    {
        _schemeEnrollmentService = schemeEnrollmentService;
    }

    // POST: api/schemeenrollment
    // Opens the SCHEME account and creates the installment schedule. Retrying
    // with the same ClientRequestId returns the original (IsReplay = true).
    [HttpPost]
    public async Task<ActionResult<SchemeEnrollmentDto>> Enroll(
        [FromBody] SchemeEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { message = "Enrollment request is required." });

        try
        {
            return Ok(await _schemeEnrollmentService.EnrollAsync(request, cancellationToken));
        }
        catch (AdvanceAccountBusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: api/schemeenrollment/5
    [HttpGet("{enrollmentGkey:int}")]
    public async Task<ActionResult<SchemeEnrollmentDto>> Get(
        int enrollmentGkey,
        CancellationToken cancellationToken)
    {
        var enrollment = await _schemeEnrollmentService.GetEnrollmentAsync(enrollmentGkey, cancellationToken);
        return enrollment is null ? NotFound() : Ok(enrollment);
    }

    // GET: api/schemeenrollment/customer/15000?includeClosed=false
    [HttpGet("customer/{customerGkey:int}")]
    public async Task<ActionResult<IEnumerable<SchemeEnrollmentDto>>> GetForCustomer(
        int customerGkey,
        [FromQuery] bool includeClosed,
        CancellationToken cancellationToken)
    {
        if (customerGkey <= 0)
            return BadRequest(new { message = "A valid customer GKey is required." });

        return Ok(await _schemeEnrollmentService.GetCustomerEnrollmentsAsync(
            customerGkey, includeClosed, cancellationToken));
    }
}
