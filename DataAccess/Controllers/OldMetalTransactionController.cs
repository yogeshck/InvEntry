using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Utils.Options;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OldMetalTransactionController
        : BaseController<OldMetalTransaction>
    {
        private readonly IRepositoryBase<VoucherType>
            _voucherTypeRepo;


        public OldMetalTransactionController(
            IRepositoryBase<OldMetalTransaction>
                oldMetalTransactionRepo,

            IRepositoryBase<VoucherType>
                voucherTypeRepo,

            IUnitOfWork
                unitOfWork)

            : base(
                oldMetalTransactionRepo,
                unitOfWork)
        {
            _voucherTypeRepo =
                voucherTypeRepo;
        }


        // ============================================================
        // FILTER BY DATE
        // ============================================================

        [HttpPost("filter")]
        public IEnumerable<OldMetalTransaction>
            FilterTrans(
                [FromBody] DateSearchOption criteria)
        {
            if (criteria is null)
            {
                return Enumerable
                    .Empty<OldMetalTransaction>();
            }

            return _repository.GetList(
                x =>
                    x.TransDate.HasValue &&
                    x.TransDate.Value.Date >=
                        criteria.From.Date &&
                    x.TransDate.Value.Date <=
                        criteria.To.Date);
        }


        // ============================================================
        // GET BY TRANSACTION NUMBER
        // ============================================================

        [HttpGet("{transNbr}")]
        public OldMetalTransaction? Get(
            string transNbr)
        {
            if (string.IsNullOrWhiteSpace(
                    transNbr))
            {
                return null;
            }

            return _repository.Get(
                x =>
                    x.TransNbr ==
                    transNbr);
        }


        // ============================================================
        // GET ALL LINES BY TRANSACTION NUMBER
        //
        // Useful for report / preview later.
        // ============================================================

        [HttpGet("lines/{transNbr}")]
        public IEnumerable<OldMetalTransaction>
            GetLines(
                string transNbr)
        {
            if (string.IsNullOrWhiteSpace(
                    transNbr))
            {
                return Enumerable
                    .Empty<OldMetalTransaction>();
            }

            return _repository.GetList(
                x =>
                    x.TransNbr ==
                    transNbr);
        }


        // ============================================================
        // GET BY DOCUMENT REFERENCE NUMBER
        // ============================================================

        [HttpGet("docRefNbr/{docRefNbr}")]
        public IEnumerable<OldMetalTransaction>
            GetByDocRefNbr(
                string docRefNbr)
        {
            if (string.IsNullOrWhiteSpace(
                    docRefNbr))
            {
                return Enumerable
                    .Empty<OldMetalTransaction>();
            }

            return _repository.GetList(
                x =>
                    x.DocRefNbr ==
                    docRefNbr);
        }


        // ============================================================
        // CREATE SINGLE OLD METAL TRANSACTION
        //
        // Keep for backward compatibility.
        //
        // BaseController Post cannot be used directly because this
        // entity requires a generated TransNbr.
        // ============================================================

        [HttpPost]
        public override async Task<ActionResult<OldMetalTransaction>>
            Post(
                [FromBody] OldMetalTransaction value)
        {
            if (value is null)
            {
                return BadRequest(
                    "Old Metal Transaction is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    value.TransType))
            {
                return BadRequest(
                    "Transaction type is required.");
            }

            try
            {
                var transactionNumber =
                    GenerateTransactionNumber(
                        value.TransType);

                value.TransNbr =
                    transactionNumber;

                _repository.Add(
                    value);

                /*
                 * VoucherType update +
                 * transaction insert are persisted
                 * by one UnitOfWork.
                 */
                await _unitOfWork
                    .SaveChangesAsync();

                return Ok(value);
            }
            catch (Exception ex)
            {
                return Problem(
                    title:
                        "Unable to save Old Metal Transaction",

                    detail:
                        ex.Message,

                    statusCode:
                        StatusCodes
                            .Status500InternalServerError);
            }
        }


        // ============================================================
        // CREATE COMPLETE OLD METAL PURCHASE
        //
        // ONE document number for ALL lines.
        // ONE UnitOfWork SaveChangesAsync().
        // ============================================================

        [HttpPost("batch")]
        public async Task<ActionResult<string>>
            PostBatch(
                [FromBody] List<OldMetalTransaction> lines)
        {
            // --------------------------------------------------------
            // BASIC REQUEST VALIDATION
            // --------------------------------------------------------

            if (lines is null ||
                lines.Count == 0)
            {
                return BadRequest(
                    "At least one Old Metal Transaction line is required.");
            }

            var firstLine =
                lines[0];

            if (firstLine is null)
            {
                return BadRequest(
                    "First Old Metal Transaction line is invalid.");
            }


            // --------------------------------------------------------
            // TRANSACTION TYPE
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(
                    firstLine.TransType))
            {
                return BadRequest(
                    "Transaction type is required.");
            }


            // --------------------------------------------------------
            // CUSTOMER
            // --------------------------------------------------------

            if (!firstLine.CustGkey.HasValue ||
                firstLine.CustGkey <= 0)
            {
                return BadRequest(
                    "A valid customer is required.");
            }


            // --------------------------------------------------------
            // VALIDATE ALL LINES BEFORE TOUCHING DATABASE
            // --------------------------------------------------------

            for (var index = 0;
                 index < lines.Count;
                 index++)
            {
                var line =
                    lines[index];

                var lineNumber =
                    index + 1;

                if (line is null)
                {
                    return BadRequest(
                        $"Line {lineNumber} is invalid.");
                }

                if (string.IsNullOrWhiteSpace(
                        line.ProductId))
                {
                    return BadRequest(
                        $"Line {lineNumber}: Product is required.");
                }

                if (line.GrossWeight
                        .GetValueOrDefault() <= 0M)
                {
                    return BadRequest(
                        $"Line {lineNumber}: Gross weight must be greater than zero.");
                }

                if (line.StoneWeight
                        .GetValueOrDefault() < 0M)
                {
                    return BadRequest(
                        $"Line {lineNumber}: Stone weight cannot be negative.");
                }

                if (line.StoneWeight
                        .GetValueOrDefault() >
                    line.GrossWeight
                        .GetValueOrDefault())
                {
                    return BadRequest(
                        $"Line {lineNumber}: Stone weight cannot exceed gross weight.");
                }

                if (line.NetWeight
                        .GetValueOrDefault() <= 0M)
                {
                    return BadRequest(
                        $"Line {lineNumber}: Net weight must be greater than zero.");
                }

                if (line.TransactedRate
                        .GetValueOrDefault() <= 0M)
                {
                    return BadRequest(
                        $"Line {lineNumber}: Transacted rate must be greater than zero.");
                }

                if (line.FinalPurchasePrice
                        .GetValueOrDefault() <= 0M)
                {
                    return BadRequest(
                        $"Line {lineNumber}: Final purchase amount must be greater than zero.");
                }
            }


            try
            {
                // ----------------------------------------------------
                // GENERATE ONLY ONE DOCUMENT NUMBER
                // ----------------------------------------------------

                var transactionNumber =
                    GenerateTransactionNumber(
                        firstLine.TransType);


                // ----------------------------------------------------
                // APPLY COMMON DOCUMENT INFORMATION
                // ----------------------------------------------------

                foreach (var line in lines)
                {
                    line.TransNbr =
                        transactionNumber;

                    /*
                     * Prevent different types accidentally
                     * being included in one purchase.
                     */
                    line.TransType =
                        firstLine.TransType;

                    /*
                     * Same customer for the whole purchase.
                     */
                    line.CustGkey =
                        firstLine.CustGkey;

                    line.CustMobile =
                        firstLine.CustMobile;

                    /*
                     * Same purchase date if another line
                     * accidentally contains a different one.
                     */
                    line.TransDate =
                        firstLine.TransDate;

                    /*
                     * Default UOM for Old Metal.
                     */
                    if (string.IsNullOrWhiteSpace(
                            line.Uom))
                    {
                        line.Uom =
                            "Grams";
                    }

                    _repository.Add(
                        line);
                }


                // ----------------------------------------------------
                // IMPORTANT
                //
                // VoucherType.LastUsedNumber update +
                // every OldMetalTransaction insert
                // are persisted together.
                // ----------------------------------------------------

                await _unitOfWork
                    .SaveChangesAsync();


                // ----------------------------------------------------
                // RETURN TRANSACTION NUMBER
                // ----------------------------------------------------

                return Ok(
                    transactionNumber);
            }
            catch (Exception ex)
            {
                return Problem(
                    title:
                        "Unable to save Old Metal Purchase",

                    detail:
                        ex.Message,

                    statusCode:
                        StatusCodes
                            .Status500InternalServerError);
            }
        }


        // ============================================================
        // UPDATE SINGLE OLD METAL TRANSACTION
        // ============================================================

        [HttpPut]
        public async Task<IActionResult> Put(
            [FromBody] OldMetalTransaction value)
        {
            if (value is null)
            {
                return BadRequest(
                    "Old Metal Transaction is required.");
            }

            try
            {
                _repository.Update(
                    value);

                await _unitOfWork
                    .SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(
                    title:
                        "Unable to update Old Metal Transaction",

                    detail:
                        ex.Message,

                    statusCode:
                        StatusCodes
                            .Status500InternalServerError);
            }
        }


        // ============================================================
        // DELETE COMPLETE OLD METAL PURCHASE
        //
        // Because multiple lines now have the SAME TransNbr,
        // deleting a document must delete all its lines.
        // ============================================================

        [HttpDelete("{transNbr}")]
        public async Task<IActionResult> Delete(
            string transNbr)
        {
            if (string.IsNullOrWhiteSpace(
                    transNbr))
            {
                return BadRequest(
                    "Transaction number is required.");
            }

            try
            {
                var transactions =
                    _repository
                        .GetList(
                            x =>
                                x.TransNbr ==
                                transNbr)
                        .ToList();

                if (transactions.Count == 0)
                {
                    return NotFound(
                        $"Old Metal Purchase '{transNbr}' was not found.");
                }

                foreach (var transaction
                         in transactions)
                {
                    _repository.Remove(
                        transaction);
                }

                await _unitOfWork
                    .SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(
                    title:
                        "Unable to delete Old Metal Purchase",

                    detail:
                        ex.Message,

                    statusCode:
                        StatusCodes
                            .Status500InternalServerError);
            }
        }


        // ============================================================
        // DOCUMENT NUMBER GENERATION
        // ============================================================

        private string GenerateTransactionNumber(
            string transactionType)
        {
            if (string.IsNullOrWhiteSpace(
                    transactionType))
            {
                throw new InvalidOperationException(
                    "Transaction type is required.");
            }

            var docType =
                _voucherTypeRepo.Get(
                    x =>
                        x.DocumentType ==
                        transactionType);

            if (docType is null)
            {
                throw new InvalidOperationException(
                    $"Voucher type '{transactionType}' was not found.");
            }

            docType.LastUsedNumber =
                docType.LastUsedNumber
                    .GetValueOrDefault()
                + 1;

            _voucherTypeRepo.Update(
                docType);

            var prefix =
                docType.DocNbrPrefix
                ?? string.Empty;

            return
                $"{prefix}" +
                $"{docType.LastUsedNumber.Value:D4}";
        }
    }
}