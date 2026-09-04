using DataAccess.Workflows;
using InvEntry.Contracts.StockTransfers;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/stock-transfers")]
[ApiController]
public class StockTransferController : ControllerBase
{
    private readonly IStockTransferWorkflow _workflow;

    public StockTransferController(IStockTransferWorkflow workflow)
    {
        _workflow = workflow;
    }

    [HttpPost]
    public async Task<ActionResult<StockTransferDetailResponse>> Create([FromBody] CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _workflow.CreateAsync(request, cancellationToken));
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockTransferListItemResponse>>> GetList(DateTime? fromDate, DateTime? toDate, string? status, string? transferType, int? toReferenceGkey, CancellationToken cancellationToken)
    {
        return Ok(await _workflow.GetListAsync(fromDate, toDate, status, transferType, toReferenceGkey, cancellationToken));
    }

    [HttpGet("{gkey:int}")]
    public async Task<ActionResult<StockTransferDetailResponse>> GetDetail(int gkey, CancellationToken cancellationToken)
    {
        var transfer = await _workflow.GetDetailAsync(gkey, cancellationToken);
        return transfer is null ? NotFound() : Ok(transfer);
    }
}