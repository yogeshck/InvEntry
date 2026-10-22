using DataAccess.Workflows;
using InvEntry.Contracts.Invoices;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InvoiceCollectionController : ControllerBase
{
    private readonly IInvoiceCollectionWorkflow _workflow;

    public InvoiceCollectionController(IInvoiceCollectionWorkflow workflow)
    {
        _workflow = workflow;
    }

    // POST api/InvoiceCollection
    [HttpPost]
    public async Task<ActionResult<SaveInvoiceCollectionResponse>> Post(
        [FromBody] SaveInvoiceCollectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _workflow.SaveAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
