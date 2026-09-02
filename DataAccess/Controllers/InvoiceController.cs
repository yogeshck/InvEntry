using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Workflows;
using InvEntry.Contracts.Invoices;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;

namespace DataAccess.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InvoiceController : ControllerBase
{
    private readonly IRepositoryBase<InvoiceHeader> _invoiceHeaderRepository;
    private readonly IRepositoryBase<VoucherType> _voucherTypeRepo;
    private readonly IInvoiceWorkflow _invoiceWorkflow;

    public InvoiceController(
        IRepositoryBase<InvoiceHeader> invoiceHeaderRepository,
        IRepositoryBase<VoucherType> voucherTypeRepo,
        IInvoiceWorkflow invoiceWorkflow)
    {
        _invoiceHeaderRepository = invoiceHeaderRepository;
        _voucherTypeRepo = voucherTypeRepo;
        _invoiceWorkflow = invoiceWorkflow;
    }

    // =========================================================
    // GET: api/invoice
    //
    // LEGACY endpoint.
    // Retained for compatibility.
    // =========================================================

    [HttpGet]
    public IEnumerable<InvoiceHeader> GetHeader()
    {
        return _invoiceHeaderRepository.GetAll();
    }

    // =========================================================
    // POST: api/invoice/filter
    //
    // Normal Invoice List.
    // IMPORTANT:
    // Only FINAL invoices are returned.
    //
    // DRAFT invoices have their own list endpoint.
    // CANCELLED invoices are not included in the normal list.
    // =========================================================

    [HttpPost("filter")]
    public IEnumerable<InvoiceHeader> FilterHeader(
        [FromBody] InvoiceSearchOption criteria)
    {
        return _invoiceHeaderRepository
            .GetList(
                x =>
                    x.Status == InvoiceStatus.Final &&
                    x.InvDate.HasValue &&
                    x.InvDate.Value.Date >= criteria.From.Date &&
                    x.InvDate.Value.Date <= criteria.To.Date)
            .OrderByDescending(x => x.InvDate)
            .ThenByDescending(x => x.Gkey);
    }

    // =========================================================
    // POST: api/invoice/drafts/filter
    //
    // Draft Invoice List.
    //
    // Supports:
    //      Date + Mobile
    //      Date only
    //      Mobile only
    //      No filter
    //
    // Always returns DRAFT invoices only.
    // =========================================================

    [HttpPost("drafts/filter")]
    public IEnumerable<InvoiceHeader> GetDraftInvoices(
        [FromBody] DateSearchOption criteria)
    {
        var hasMobile =
            !string.IsNullOrWhiteSpace(criteria.Filter1);

        var hasDateRange =
            criteria.From != DateTime.MinValue &&
            criteria.To != DateTime.MinValue;

        IEnumerable<InvoiceHeader> result;

        // -----------------------------------------------------
        // Date + Mobile
        // -----------------------------------------------------

        if (hasDateRange && hasMobile)
        {
            result =
                _invoiceHeaderRepository.GetList(
                    x =>
                        x.Status == InvoiceStatus.Draft &&
                        x.InvDate.HasValue &&
                        x.InvDate.Value.Date >= criteria.From.Date &&
                        x.InvDate.Value.Date <= criteria.To.Date &&
                        x.CustMobile == criteria.Filter1);
        }

        // -----------------------------------------------------
        // Date only
        // -----------------------------------------------------

        else if (hasDateRange)
        {
            result =
                _invoiceHeaderRepository.GetList(
                    x =>
                        x.Status == InvoiceStatus.Draft &&
                        x.InvDate.HasValue &&
                        x.InvDate.Value.Date >= criteria.From.Date &&
                        x.InvDate.Value.Date <= criteria.To.Date);
        }

        // -----------------------------------------------------
        // Mobile only
        // -----------------------------------------------------

        else if (hasMobile)
        {
            result =
                _invoiceHeaderRepository.GetList(
                    x =>
                        x.Status == InvoiceStatus.Draft &&
                        x.CustMobile == criteria.Filter1);
        }

        // -----------------------------------------------------
        // No filter
        // -----------------------------------------------------

        else
        {
            result =
                _invoiceHeaderRepository.GetList(
                    x =>
                        x.Status == InvoiceStatus.Draft);
        }

        return result
            .OrderByDescending(x => x.InvDate)
            .ThenByDescending(x => x.Gkey);
    }

    // =========================================================
    // POST: api/invoice/getInvList
    //
    // Existing customer invoice lookup.
    //
    // IMPORTANT:
    // Only FINAL invoices should participate here.
    // Drafts are handled separately.
    // =========================================================

    [HttpPost("getInvList")]
    public IEnumerable<InvoiceHeader> GetInvList(
        [FromBody] DateSearchOption criteria)
    {
        var hasMobile =
            !string.IsNullOrWhiteSpace(criteria.Filter1);

        var hasDateRange =
            criteria.From != DateTime.MinValue &&
            criteria.To != DateTime.MinValue;

        // -----------------------------------------------------
        // Date + Mobile
        // -----------------------------------------------------

        if (hasMobile && hasDateRange)
        {
            return _invoiceHeaderRepository
                .GetList(
                    x =>
                        x.Status == InvoiceStatus.Final &&
                        x.InvDate.HasValue &&
                        x.InvDate.Value.Date >= criteria.From.Date &&
                        x.InvDate.Value.Date <= criteria.To.Date &&
                        x.CustMobile == criteria.Filter1)
                .OrderBy(x => x.InvDate)
                .ThenBy(x => x.Gkey);
        }

        // -----------------------------------------------------
        // Mobile only
        // -----------------------------------------------------

        if (hasMobile)
        {
            return _invoiceHeaderRepository
                .GetList(
                    x =>
                        x.Status == InvoiceStatus.Final &&
                        x.CustMobile == criteria.Filter1)
                .OrderBy(x => x.InvDate)
                .ThenBy(x => x.Gkey);
        }

        // -----------------------------------------------------
        // Date only
        //
        // This makes the endpoint safer than the old version,
        // which could accidentally search CustMobile == null.
        // -----------------------------------------------------

        if (hasDateRange)
        {
            return _invoiceHeaderRepository
                .GetList(
                    x =>
                        x.Status == InvoiceStatus.Final &&
                        x.InvDate.HasValue &&
                        x.InvDate.Value.Date >= criteria.From.Date &&
                        x.InvDate.Value.Date <= criteria.To.Date)
                .OrderBy(x => x.InvDate)
                .ThenBy(x => x.Gkey);
        }

        // -----------------------------------------------------
        // No criteria
        // -----------------------------------------------------

        return _invoiceHeaderRepository
            .GetList(
                x => x.Status == InvoiceStatus.Final)
            .OrderBy(x => x.InvDate)
            .ThenBy(x => x.Gkey);
    }

    // =========================================================
    // POST: api/invoice/outstanding
    //
    // Outstanding receivables.
    //
    // CRITICAL:
    // Only FINAL invoices are receivables.
    //
    // DRAFT invoices MUST NOT participate in accounting /
    // outstanding calculations.
    // =========================================================

    [HttpPost("outstanding")]
    public IEnumerable<InvoiceHeader> GetOutstanding(
        [FromBody] DateSearchOption criteria)
    {
        var hasMobile =
            !string.IsNullOrWhiteSpace(criteria.Filter1);

        var hasDateRange =
            criteria.From != DateTime.MinValue &&
            criteria.To != DateTime.MinValue;

        // -----------------------------------------------------
        // Date + Mobile
        // -----------------------------------------------------

        if (hasMobile && hasDateRange)
        {
            return _invoiceHeaderRepository
                .GetList(
                    x =>
                        x.Status == InvoiceStatus.Final &&
                        x.InvDate.HasValue &&
                        x.InvDate.Value.Date >= criteria.From.Date &&
                        x.InvDate.Value.Date <= criteria.To.Date &&
                        x.CustMobile == criteria.Filter1 &&
                        x.InvBalance > 0)
                .OrderBy(x => x.InvDate)
                .ThenBy(x => x.Gkey);
        }

        // -----------------------------------------------------
        // Date only
        // -----------------------------------------------------

        if (hasDateRange)
        {
            return _invoiceHeaderRepository
                .GetList(
                    x =>
                        x.Status == InvoiceStatus.Final &&
                        x.InvDate.HasValue &&
                        x.InvDate.Value.Date >= criteria.From.Date &&
                        x.InvDate.Value.Date <= criteria.To.Date &&
                        x.InvBalance > 0)
                .OrderBy(x => x.InvDate)
                .ThenBy(x => x.Gkey);
        }

        // -----------------------------------------------------
        // Mobile only
        // -----------------------------------------------------

        if (hasMobile)
        {
            return _invoiceHeaderRepository
                .GetList(
                    x =>
                        x.Status == InvoiceStatus.Final &&
                        x.CustMobile == criteria.Filter1 &&
                        x.InvBalance > 0)
                .OrderBy(x => x.InvDate)
                .ThenBy(x => x.Gkey);
        }

        // -----------------------------------------------------
        // No filter
        // -----------------------------------------------------

        return _invoiceHeaderRepository
            .GetList(
                x =>
                    x.Status == InvoiceStatus.Final &&
                    x.InvBalance > 0)
            .OrderBy(x => x.InvDate)
            .ThenBy(x => x.Gkey);
    }

    // =========================================================
    // GET: api/invoice/{invNbr}
    //
    // LEGACY lookup by Invoice Number.
    //
    // Retain current route for compatibility.
    // =========================================================

    [HttpGet("{invNbr}")]
    public InvoiceHeader? Get(string invNbr)
    {
        return _invoiceHeaderRepository.Get(
            x => x.InvNbr == invNbr);
    }

    // =========================================================
    // GET: api/invoice/{gkey}/edit
    //
    // Loads the complete editable Draft aggregate:
    //
    //      Header
    //      Lines
    //      Old Metal
    //      Receipts
    //
    // GetForEditAsync() validates that the invoice is DRAFT.
    // =========================================================

    [HttpGet("{gkey:int}/edit")]
    public async Task<ActionResult<InvoiceEditResponse>> GetForEdit(
        int gkey,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _invoiceWorkflow.GetForEditAsync(
                    gkey,
                    cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // =========================================================
    // POST: api/invoice/draft
    //
    // CANONICAL NEW SAVE ENDPOINT.
    //
    // Creates or updates a DRAFT invoice.
    //
    // This endpoint:
    //      Saves Header
    //      Saves Lines
    //      Saves Old Metal
    //      Saves Payment details
    //
    // It DOES NOT:
    //      Generate final Invoice Number
    //      Update ProductStock
    //      Insert StockMovement
    //      Post final accounting
    //      Print final invoice
    // =========================================================

    [HttpPost("draft")]
    public async Task<ActionResult<SaveInvoiceResponse>> SaveDraft(
        [FromBody] SaveInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(
                "Invoice request is required.");
        }

        if (request.Header is null)
        {
            return BadRequest(
                "Invoice header is required.");
        }

        if (request.Lines is null ||
            request.Lines.Count == 0)
        {
            return BadRequest(
                "Invoice must contain at least one line.");
        }

        try
        {
            var result =
                await _invoiceWorkflow.SaveDraftAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // =========================================================
    // POST: api/invoice/save
    //
    // LEGACY AGGREGATE SAVE ENDPOINT.
    //
    // Retained temporarily so existing client code does not
    // break during migration.
    //
    // New WPF code should use:
    //
    //      POST api/invoice/draft
    //
    // =========================================================

    [Obsolete("Use POST api/invoice/draft.")]
    [HttpPost("save")]
    public Task<ActionResult<SaveInvoiceResponse>> Save(
        [FromBody] SaveInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        return SaveDraft(
            request,
            cancellationToken);
    }

    // =========================================================
    // OLD POST: api/invoice
    //
    // LEGACY ONLY.
    //
    // WARNING:
    // This generates a FINAL-style Invoice Number immediately.
    //
    // Keep temporarily because old WPF code may still call it.
    //
    // DO NOT use from the new Invoice lifecycle.
    // =========================================================

    [HttpPost]
    public InvoiceHeader Post(
        [FromBody] InvoiceHeader value)
    {
        var voucherType =
            _voucherTypeRepo.Get(
                x => x.DocumentType == "Sale Invoice");

        if (voucherType is null)
        {
            throw new InvalidOperationException(
                "Voucher type 'Sale Invoice' was not found.");
        }

        voucherType.LastUsedNumber++;

        _voucherTypeRepo.Update(
            voucherType);

        var documentPrefixFormat =
            voucherType.DocNbrPrefix;

        value.InvNbr =
            string.Format(
                "{0}{1}",
                documentPrefixFormat,
                voucherType.LastUsedNumber?
                    .ToString(
                        $"D{voucherType.DocNbrLength}"));

        value.CreatedOn =
            DateTime.Now;

        _invoiceHeaderRepository.Add(
            value);

        return value;
    }

    // =========================================================
    // OLD PUT: api/invoice/{invNbr}
    //
    // LEGACY ONLY.
    //
    // Keep temporarily.
    //
    // New Draft editing must go through SaveDraftAsync().
    // =========================================================

    [HttpPut("{invNbr}")]
    public void Put(
        string invNbr,
        [FromBody] InvoiceHeader value)
    {
        value.InvNbr =
            invNbr;

        value.ModifiedOn =
            DateTime.Now;

        _invoiceHeaderRepository.Update(
            value);
    }

    private async Task<string> GenerateInvoiceNumberAsync(
    CancellationToken cancellationToken)
    {
        var voucherType =
            _voucherTypeRepo.Get(
                x => x.DocumentType == "Sale Invoice");

        if (voucherType == null)
        {
            throw new InvalidOperationException(
                "Invoice voucher type configuration was not found.");
        }

        var nextNumber =
            voucherType.LastUsedNumber + 1;

        var numberLength =
            voucherType.DocNbrLength;

        var prefix =
            voucherType.DocNbrPrefix ?? string.Empty;

        var numericPart =
            nextNumber
                .ToString()
                .PadLeft(
                    (int)numberLength,
                    '0');

        voucherType.LastUsedNumber =
            nextNumber;

        return prefix + numericPart;
    }

    [HttpPost("{invoiceGkey:int}/finalise")]
    public async Task<ActionResult<FinaliseInvoiceResponse>> Finalise(
        int invoiceGkey,
        [FromBody] FinaliseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (invoiceGkey <= 0)
            return BadRequest("A valid invoice GKey is required.");

        if (request is null)
            return BadRequest("Finalisation request is required.");

        if (request.InvoiceGkey <= 0)
            return BadRequest("Invoice GKey is required.");

        if (invoiceGkey != request.InvoiceGkey)
            return BadRequest(
                "Invoice GKey in the URL does not match the request.");

        var result =
            await _invoiceWorkflow.FinaliseAsync(
                request,
                cancellationToken);

        return Ok(result);
    }

    // =========================================================
    // DELETE: api/invoice/{invNbr}
    //
    // LEGACY ONLY.
    //
    // Eventually this should NOT be used for invoice lifecycle.
    //
    // Draft cancellation will use:
    //
    //      POST api/invoice/{gkey}/cancel
    //
    // rather than physically deleting an invoice.
    // =========================================================

    [HttpDelete("{invNbr}")]
    public void Delete(
        string invNbr)
    {
        var invoice =
            Get(invNbr);

        if (invoice is null)
            return;

        _invoiceHeaderRepository.Remove(
            invoice);
    }
}