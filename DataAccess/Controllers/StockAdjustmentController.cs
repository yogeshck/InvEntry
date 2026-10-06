using DataAccess.Workflows;
using InvEntry.Contracts.StockAdjustments;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/stock-adjustments")]
[ApiController]
public sealed class StockAdjustmentController : ControllerBase
{
    private readonly IStockAdjustmentWorkflow _workflow;

    public StockAdjustmentController(
        IStockAdjustmentWorkflow workflow)
    {
        _workflow = workflow;
    }

    [HttpPost]
    public async Task<ActionResult<StockAdjustmentResponse>> Create(
        [FromBody] CreateStockAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _workflow.CreateAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{gkey:int}")]
    public async Task<ActionResult<StockAdjustmentResponse>> GetDetail(
        int gkey,
        CancellationToken cancellationToken)
    {
        var result =
            await _workflow.GetDetailAsync(
                gkey,
                cancellationToken);

        return result == null
            ? NotFound()
            : Ok(result);
    }
}
