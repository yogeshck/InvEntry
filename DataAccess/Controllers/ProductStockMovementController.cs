using DataAccess.Services.StockMovement;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/product-stock-movement")]
[ApiController]
public sealed class ProductStockMovementController : ControllerBase
{
    private readonly IProductStockMovementService _stockMovementService;
    private readonly ILogger<ProductStockMovementController> _logger;

    public ProductStockMovementController(
        IProductStockMovementService stockMovementService,
        ILogger<ProductStockMovementController> logger)
    {
        _stockMovementService = stockMovementService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Post(
        [FromBody] StockMovementRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var movement = await _stockMovementService.ApplyAsync(
                request,
                cancellationToken);

            return Ok(movement);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid stock movement request for {DocumentType} {DocumentNbr}.",
                request.DocumentType,
                request.DocumentNbr);

            return BadRequest(new
            {
                error = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to process stock movement for {DocumentType} {DocumentNbr}.",
                request.DocumentType,
                request.DocumentNbr);

            return Conflict(new
            {
                error = ex.Message
            });
        }
    }
}