using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomerController : ControllerBase
{
    private readonly IRepositoryBase<OrgCustomer> _customer;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerController(
        IRepositoryBase<OrgCustomer> customerRepo,
        IUnitOfWork unitOfWork)
    {
        _customer = customerRepo;
        _unitOfWork = unitOfWork;
    }

    // GET api/customer
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_customer.GetAll());
    }

    // GET api/customer/{mobile}
    [HttpGet("{mobile}")]
    public IActionResult Get(string mobile)
    {
        var customer =
            _customer.Get(
                x => x.MobileNbr == mobile);

        if (customer is null)
            return NotFound();

        return Ok(customer);
    }

    // GET api/customer/by-gkey/123
    [HttpGet("by-gkey/{gkey:int}")]
    public IActionResult GetByGkey(int gkey)
    {
        if (gkey <= 0)
            return BadRequest("Invalid customer GKey.");

        var customer =
            _customer.Get(
                x => x.Gkey == gkey);

        if (customer is null)
            return NotFound();

        return Ok(customer);
    }

    // POST api/customer
    [HttpPost]
    public async Task<ActionResult<OrgCustomer>> Post(
        [FromBody] OrgCustomer value,
        CancellationToken cancellationToken)
    {
        if (value is null)
            return BadRequest("Customer is required.");

        if (string.IsNullOrWhiteSpace(value.MobileNbr))
        {
            return BadRequest(
                "Customer mobile number is required.");
        }

        value.MobileNbr =
            value.MobileNbr.Trim();

        //
        // Cycle-1 duplicate protection.
        //
        var existing =
            await _customer.GetAsync(
                x => x.MobileNbr == value.MobileNbr);

        if (existing is not null)
        {
            //
            // POST is CREATE only.
            //
            // Do not silently update an existing customer.
            //
            return Ok(existing);
        }

        _customer.Add(value);

        //
        // IMPORTANT:
        // Repository.Add() only tracks the entity.
        // SaveChanges generates the identity Gkey.
        //
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        if (value.Gkey <= 0)
        {
            throw new InvalidOperationException(
                "Customer was saved but no GKey was generated.");
        }

        return Ok(value);
    }

    // PUT api/customer/{mobileNbr}
    [HttpPut("{mobileNbr}")]
    public async Task<IActionResult> Put(
        string mobileNbr,
        [FromBody] OrgCustomer value,
        CancellationToken cancellationToken)
    {
        if (value is null)
            return BadRequest("Customer is required.");

        if (value.Gkey <= 0)
        {
            return BadRequest(
                "Customer GKey is required for update.");
        }

        var existing =
            await _customer.GetAsync(
                x => x.Gkey == value.Gkey);

        if (existing is null)
            return NotFound();

        //
        // For now Update() is retained.
        // Later we can move this mapping into
        // CustomerWorkflow.
        //
        _customer.Update(value);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Ok(value);
    }

    // DELETE api/customer/{mobileNbr}
    [HttpDelete("{mobileNbr}")]
    public async Task<IActionResult> Delete(
        string mobileNbr,
        CancellationToken cancellationToken)
    {
        var customer =
            await _customer.GetAsync(
                x => x.MobileNbr == mobileNbr);

        if (customer is null)
            return NotFound();

        _customer.Remove(customer);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return NoContent();
    }
}