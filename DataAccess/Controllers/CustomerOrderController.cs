using InvEntry.Contracts.CustomerOrders;
using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Workflows;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomerOrderController : ControllerBase
{
    private readonly IRepositoryBase<CustomerOrder>
        _customerOrderRepository;

    private readonly ICustomerOrderWorkflow
        _customerOrderWorkflow;

    public CustomerOrderController(
        IRepositoryBase<CustomerOrder> customerOrderRepository,
        ICustomerOrderWorkflow customerOrderWorkflow)
    {
        _customerOrderRepository =
            customerOrderRepository;

        _customerOrderWorkflow =
            customerOrderWorkflow;
    }

    // GET api/customerOrder
    [HttpGet]
    public ActionResult<IEnumerable<CustomerOrder>> GetAll()
    {
        var orders =
            _customerOrderRepository.GetAll();

        return Ok(orders);
    }

    [HttpPost("filter")]
    public async Task<ActionResult<IEnumerable<CustomerOrder>>> Filter(
    [FromBody] DateSearchOption criteria)
    {
        var search = criteria.Filter1?.Trim();

        var orders =
            await _customerOrderRepository.GetListAsync(
                x =>
                    // Date range
                    x.OrderDate.HasValue &&
                    x.OrderDate.Value.Date >= criteria.From.Date &&
                    x.OrderDate.Value.Date <= criteria.To.Date &&

                    // Optional search
                    (
                        string.IsNullOrEmpty(search) ||

                        (x.OrderNbr != null &&
                         x.OrderNbr.Contains(search)) ||

                        (x.CustMobileNbr != null &&
                         x.CustMobileNbr.Contains(search)) ||

                        (x.OrderRefNbr != null &&
                         x.OrderRefNbr.Contains(search))
                    ));

        return Ok(orders);
    }

    // GET api/customerOrder/B000123
    [HttpGet("{orderNbr}")]
    public ActionResult<CustomerOrder> Get(
        string orderNbr)
    {
        var order =
            _customerOrderRepository.Get(
                x => x.OrderNbr == orderNbr);

        if (order is null)
            return NotFound();

        return Ok(order);
    }

    // POST api/customerOrder/save
    [HttpPost("save")]
    public async Task<ActionResult<SaveCustomerOrderResponse>> Save(
        [FromBody] SaveCustomerOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Header == null)
            {
                return BadRequest(
                    "Customer order header is required.");
            }

            if (request.Lines == null ||
                request.Lines.Count == 0)
            {
                return BadRequest(
                    "At least one customer order line is required.");
            }

            var result =
                await _customerOrderWorkflow.SaveAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}