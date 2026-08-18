using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AddressController : ControllerBase
{
    private readonly IRepositoryBase<OrgAddress> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddressController(
        IRepositoryBase<OrgAddress> addressRepo,
        IUnitOfWork unitOfWork)
    {
        _repository = addressRepo;
        _unitOfWork = unitOfWork;
    }

    // GET api/address/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrgAddress>> Get(
        int id)
    {
        if (id <= 0)
            return BadRequest("Invalid address GKey.");

        var address =
            await _repository.GetAsync(
                x => x.Gkey == id);

        if (address is null)
            return NotFound();

        return Ok(address);
    }

    // POST api/address/address
    //
    // Retaining the existing route so that
    // CustomerService.CreateCustomer() continues
    // to work without client-side changes.
    [HttpPost("address")]
    public async Task<ActionResult<OrgAddress>> Post(
        [FromBody] OrgAddress value,
        CancellationToken cancellationToken)
    {
        if (value is null)
            return BadRequest("Address is required.");

        //
        // New address
        //
        if (value.Gkey <= 0)
        {
            _repository.Add(value);

            //
            // Required so SQL Server generates Gkey.
            //
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            if (value.Gkey <= 0)
            {
                throw new InvalidOperationException(
                    "Address was saved but no GKey was generated.");
            }

            return Ok(value);
        }

        //
        // Existing address
        //
        var existing =
            await _repository.GetAsync(
                x => x.Gkey == value.Gkey);

        if (existing is null)
        {
            return NotFound(
                $"Address GKey {value.Gkey} was not found.");
        }

        MapAddress(
            value,
            existing);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Ok(existing);
    }

    // PUT api/address/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(
        int id,
        [FromBody] OrgAddress value,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
            return BadRequest("Invalid address GKey.");

        var existing =
            await _repository.GetAsync(
                x => x.Gkey == id);

        if (existing is null)
            return NotFound();

        MapAddress(
            value,
            existing);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Ok(existing);
    }

    private static void MapAddress(
        OrgAddress source,
        OrgAddress target)
    {
        target.AddressLine1 =
            source.AddressLine1;

        target.AddressLine2 =
            source.AddressLine2;

        target.AddressLine3 =
            source.AddressLine3;

        target.Area =
            source.Area;

        target.City =
            source.City;

        target.District =
            source.District;

        target.State =
            source.State;

        target.Country =
            source.Country;

        target.Pincode =
            source.Pincode;

        target.GstStateCode =
            source.GstStateCode;

        target.TenantGkey =
            source.TenantGkey;

        //
        // Do NOT copy Gkey.
        //
        // Existing entity identity is controlled
        // by the database.
        //
    }
}