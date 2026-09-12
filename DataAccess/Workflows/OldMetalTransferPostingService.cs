using DataAccess.Models;
using DataAccess.Services;

namespace DataAccess.Workflows;

public sealed class OldMetalTransferPostingService
    : IOldMetalTransferPostingService
{
    private const string DocumentType =
        "OM Transfer";

    private const string DocumentReferenceType =
        "Stock Transfer";

    private const int RemarksLength =
        50;


    private readonly MijmsContext _context;

    private readonly IVoucherNumberService
        _voucherNumberService;


    public OldMetalTransferPostingService(
        MijmsContext context,
        IVoucherNumberService voucherNumberService)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));

        _voucherNumberService =
            voucherNumberService
            ?? throw new ArgumentNullException(
                nameof(voucherNumberService));
    }


    public async Task PostAsync(
        StockTransferHeader header,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            header);

        cancellationToken
            .ThrowIfCancellationRequested();


        // ============================================================
        // BASIC VALIDATION
        // ============================================================

        if (header.Lines is null ||
            header.Lines.Count == 0)
        {
            throw new InvalidOperationException(
                "Old Metal Transfer must contain at least one line.");
        }


        // ============================================================
        // RESOLVE DESTINATION CUSTOMER
        // ============================================================

        /*
         * Destination RefValue represents the customer/mobile
         * mapping used by the legacy Old Metal transaction system.
         *
         * This is retained only for compatibility with the existing
         * OLD_METAL_TRANSACTION structure.
         */
        var customer =
            _context.OrgCustomers
                .SingleOrDefault(
                    x =>
                        x.MobileNbr ==
                        header.ToReferenceValue);


        if (customer is null)
        {
            throw new InvalidOperationException(
                "Destination reference does not resolve to a valid " +
                "customer required for Old Metal transfer posting.");
        }


        // ============================================================
        // VALIDATE ALL TRANSFER LINES
        // ============================================================

        ValidateLines(
            header.Lines);


        // ============================================================
        // GENERATE ONE OM TRANSFER DOCUMENT NUMBER
        // ============================================================

        /*
         * IMPORTANT:
         *
         * ONE OM Transfer transaction number per Stock Transfer.
         *
         * Example:
         *
         * Stock Transfer:
         *     ST-00025
         *
         * Lines:
         *     GOLD   916
         *     GOLD   750
         *     SILVER 925
         *
         * Compatibility transactions:
         *
         *     TransNbr = OMT-xxxxx
         *
         * The SAME TransNbr is assigned to every old-metal
         * line belonging to this Stock Transfer.
         */
        var transactionNumber =
            await _voucherNumberService
                .GetNextNumberAsync(
                    DocumentType,
                    cancellationToken);


        if (string.IsNullOrWhiteSpace(
                transactionNumber))
        {
            throw new InvalidOperationException(
                "Unable to generate OM Transfer transaction number.");
        }


        // ============================================================
        // CREATE OLD METAL COMPATIBILITY TRANSACTIONS
        // ============================================================

        foreach (var line in header.Lines)
        {
            cancellationToken
                .ThrowIfCancellationRequested();


            var transaction =
                new OldMetalTransaction
                {
                    // ------------------------------------------------
                    // OM TRANSFER DOCUMENT
                    // ------------------------------------------------

                    TransNbr =
                        transactionNumber,

                    TransDate =
                        header.TransferDate,

                    TransType =
                        DocumentType,


                    // ------------------------------------------------
                    // STOCK TRANSFER REFERENCE
                    // ------------------------------------------------

                    /*
                     * Link every compatibility transaction back
                     * to the dedicated Stock Transfer document.
                     */
                    DocRefGkey =
                        header.Gkey,

                    DocRefNbr =
                        header.TransferNbr,

                    DocRefDate =
                        header.TransferDate,

                    DocRefType =
                        DocumentReferenceType,


                    // ------------------------------------------------
                    // DESTINATION
                    // ------------------------------------------------

                    CustGkey =
                        customer.Gkey,

                    CustMobile =
                        customer.MobileNbr,


                    // ------------------------------------------------
                    // PRODUCT
                    // ------------------------------------------------

                    ProductGkey =
                        line.ProductGkey,

                    ProductId =
                        line.ProductId,

                    ProductCategory =
                        line.ProductCategory,

                    Metal =
                        line.Metal,

                    Purity =
                        line.Purity,

                    Uom =
                        line.Uom,


                    // ------------------------------------------------
                    // WEIGHT
                    // ------------------------------------------------

                    /*
                     * OM Transfer represents physical movement only.
                     *
                     * For the current transfer UI:
                     *
                     * GrossWeight = transferred net weight
                     * StoneWeight = 0
                     * NetWeight   = transferred net weight
                     */
                    GrossWeight =
                        line.GrossWeight,

                    StoneWeight =
                        line.StoneWeight,

                    NetWeight =
                        line.NetWeight,


                    // ------------------------------------------------
                    // NO COMMERCIAL VALUE FOR OM TRANSFER
                    // ------------------------------------------------

                    /*
                     * IMPORTANT:
                     *
                     * OM Purchase requires:
                     *
                     *     TransactedRate
                     *     FinalPurchasePrice
                     *
                     * OM Transfer does NOT.
                     *
                     * This document merely transfers old-metal
                     * physical stock to the melting destination.
                     *
                     * Therefore monetary fields must remain NULL.
                     */
                    TransactedRate =
                        null,

                    TotalProposedPrice =
                        null,

                    FinalPurchasePrice =
                        null,


                    // ------------------------------------------------
                    // REMARKS
                    // ------------------------------------------------

                    Remarks =
                        line.Notes
                };


            _context
                .OldMetalTransactions
                .Add(
                    transaction);
        }


        // ============================================================
        // IMPORTANT - DO NOT SAVE HERE
        // ============================================================

        /*
         * DO NOT call SaveChanges here.
         *
         * StockTransferWorkflow owns the database transaction.
         *
         * The parent workflow controls:
         *
         *     Stock Transfer Header
         *     Stock Transfer Lines
         *     OM Transfer compatibility transactions
         *     Stock movement
         *     Voucher number updates
         *
         * and finally:
         *
         *     SaveChanges
         *     Commit / Rollback
         *
         * This ensures the entire Stock Transfer remains atomic.
         */
    }


    // ================================================================
    // VALIDATE OM TRANSFER LINES
    // ================================================================

    private static void ValidateLines(
        IEnumerable<StockTransferLine> lines)
    {
        foreach (var line in lines)
        {
            // --------------------------------------------------------
            // PRODUCT
            // --------------------------------------------------------

            if (!line.ProductGkey.HasValue ||
                line.ProductGkey.Value <= 0)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Product GKey is required.");
            }


            if (string.IsNullOrWhiteSpace(
                    line.ProductId))
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Product is required.");
            }


            if (string.IsNullOrWhiteSpace(
                    line.Metal))
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Metal is required.");
            }


            if (string.IsNullOrWhiteSpace(
                    line.Purity))
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Purity is required.");
            }


            if (string.IsNullOrWhiteSpace(
                    line.Uom))
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: UOM is required.");
            }


            // --------------------------------------------------------
            // WEIGHT
            // --------------------------------------------------------

            if (line.GrossWeight <= 0M)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Gross weight must be greater than zero.");
            }


            if (line.StoneWeight < 0M)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Stone weight cannot be negative.");
            }


            if (line.StoneWeight >
                line.GrossWeight)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Stone weight cannot exceed gross weight.");
            }


            if (line.NetWeight <= 0M)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Net weight must be greater than zero.");
            }


            var expectedNet =
                line.GrossWeight -
                line.StoneWeight;


            if (Math.Abs(
                    expectedNet -
                    line.NetWeight) >
                0.001M)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Net weight must equal " +
                    "Gross Weight - Stone Weight.");
            }


            // --------------------------------------------------------
            // COMMERCIAL VALUES
            // --------------------------------------------------------

            /*
             * DO NOT validate:
             *
             *     TransactedRate
             *     TransferValue
             *
             * This service is specifically for:
             *
             *     TransType = "OM Transfer"
             *
             * OM Transfer is a physical stock movement only.
             *
             * Rate / purchase amount validation belongs to
             * "OM Purchase" and remains in the Old Metal
             * Purchase workflow/controller.
             */


            // --------------------------------------------------------
            // NOTES
            // --------------------------------------------------------

            if (line.Notes?.Length >
                RemarksLength)
            {
                throw new InvalidOperationException(
                    $"Line {line.LineNbr}: Notes cannot exceed " +
                    $"{RemarksLength} characters.");
            }
        }
    }
}