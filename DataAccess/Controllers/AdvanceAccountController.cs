using DataAccess.Services.Advances;
using InvEntry.Contracts.Advances;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

/// <summary>
/// Customer advance and RD accounts. Balances are changed only here and by
/// invoice finalisation; LEDGERS_HEADER is no longer the balance of record.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AdvanceAccountController : ControllerBase
{
    private readonly IAdvanceAccountService _advanceAccountService;

    public AdvanceAccountController(IAdvanceAccountService advanceAccountService)
    {
        _advanceAccountService = advanceAccountService;
    }

    // =========================================================
    // POST: api/advanceaccount/receipt
    //
    // Advance / RD money received. Creates the VOUCHER, the
    // ADVANCE_LEDGER RECEIPT row and updates the account balance
    // in one transaction. Retrying with the same ClientRequestId
    // returns the original receipt (IsReplay = true).
    // =========================================================

    [HttpPost("receipt")]
    public async Task<ActionResult<AdvanceReceiptResponse>> CreateReceipt(
        [FromBody] AdvanceReceiptRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { message = "Receipt request is required." });

        try
        {
            return Ok(await _advanceAccountService.CreateReceiptAsync(
                request, cancellationToken));
        }
        catch (AdvanceAccountBusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Voucher type missing or inactive in VOUCHER_TYPES.
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================================================
    // GET: api/advanceaccount/customer/{customerGkey}/available
    //
    // Active advance / RD accounts with a balance, for the billing
    // settlement screen. SettlementMode tells which payment mode
    // ("Advance Adj" / "RD Adj") uses the account.
    // =========================================================

    [HttpGet("customer/{customerGkey:int}/available")]
    public async Task<ActionResult<IEnumerable<AdvanceAccountSummary>>> GetAvailable(
        int customerGkey,
        CancellationToken cancellationToken)
    {
        if (customerGkey <= 0)
            return BadRequest(new { message = "A valid customer GKey is required." });

        return Ok(await _advanceAccountService.GetAvailableAccountsAsync(
            customerGkey, cancellationToken));
    }
}
