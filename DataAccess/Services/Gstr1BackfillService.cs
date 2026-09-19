using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Contracts.Gst;
using InvEntry.Contracts.Invoices;

namespace DataAccess.Services;

public sealed class Gstr1BackfillService
    : IGstr1BackfillService
{
    private readonly IRepositoryBase<InvoiceHeader>
        _invoiceRepository;

    private readonly IRepositoryBase<InvoiceLine>
        _lineRepository;

    private readonly IRepositoryBase<GstGstr1Document>
        _gstDocumentRepository;

    private readonly IGstr1StagingService
        _stagingService;

    private readonly IUnitOfWork
        _unitOfWork;

    public Gstr1BackfillService(
        IRepositoryBase<InvoiceHeader> invoiceRepository,
        IRepositoryBase<InvoiceLine> lineRepository,
        IRepositoryBase<GstGstr1Document> gstDocumentRepository,
        IGstr1StagingService stagingService,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository =
            invoiceRepository;

        _lineRepository =
            lineRepository;

        _gstDocumentRepository =
            gstDocumentRepository;

        _stagingService =
            stagingService;

        _unitOfWork =
            unitOfWork;
    }

    public async Task<Gstr1BackfillResponse> BackfillAsync(
        Gstr1BackfillRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var requestedGstin =
            NormalizeGstin(
                request.SupplierGstin);

        var configuredGstin =
            NormalizeGstin(
                _stagingService.GetSupplierGstin());

        if (!string.Equals(
                requestedGstin,
                configuredGstin,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Requested GSTIN '{requestedGstin}' does not match " +
                $"the current company's GSTIN '{configuredGstin}'.");
        }

        var fromDate =
            request.FromDate.Date;

        var toDate =
            request.ToDate.Date;

        var response =
            new Gstr1BackfillResponse
            {
                SupplierGstin =
                    configuredGstin,

                FromDate =
                    fromDate,

                ToDate =
                    toDate,

                DryRun =
                    request.DryRun
            };

        // =========================================================
        // FIND FINAL INVOICES
        // =========================================================

        var invoices =
            _invoiceRepository
                .GetList(
                    x =>
                        x.Status == InvoiceStatus.Final &&
                        x.InvDate.HasValue &&
                        x.InvDate.Value.Date >= fromDate &&
                        x.InvDate.Value.Date <= toDate)
                .OrderBy(
                    x => x.InvDate)
                .ThenBy(
                    x => x.Gkey)
                .ToList();

        response.ExaminedCount =
            invoices.Count;

        // =========================================================
        // PROCESS ONE INVOICE AT A TIME
        // =========================================================

        foreach (var invoice in invoices)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var item =
                new Gstr1BackfillItemResponse
                {
                    SourceGkey =
                        invoice.Gkey,

                    DocumentNbr =
                        invoice.InvNbr ??
                        string.Empty,

                    DocumentDate =
                        invoice.InvDate
                };

            response.Items.Add(
                item);

            try
            {
                // -------------------------------------------------
                // BASIC HISTORICAL DATA CHECKS
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(
                        invoice.InvNbr))
                {
                    item.Outcome =
                        "Skipped";

                    item.Message =
                        "Final invoice has no invoice number.";

                    response.SkippedCount++;

                    continue;
                }

                if (!invoice.InvDate.HasValue)
                {
                    item.Outcome =
                        "Skipped";

                    item.Message =
                        "Final invoice has no invoice date.";

                    response.SkippedCount++;

                    continue;
                }

                // -------------------------------------------------
                // CHECK WHETHER ALREADY STAGED
                // -------------------------------------------------

                var existing =
                    _gstDocumentRepository.Get(
                        x =>
                            x.SourceGkey ==
                                invoice.Gkey &&
                            x.DocumentType ==
                                "SalesInvoice" &&
                            x.SupplierGstin ==
                                configuredGstin);

                if (existing != null)
                {
                    item.Outcome =
                        "AlreadyStaged";

                    item.Message =
                        $"GST staging document {existing.Gkey} already exists.";

                    response.AlreadyStagedCount++;

                    continue;
                }

                // -------------------------------------------------
                // LOAD INVOICE LINES
                // -------------------------------------------------

                var lines =
                    _lineRepository
                        .GetList(
                            x =>
                                x.InvoiceHdrGkey ==
                                invoice.Gkey)
                        .OrderBy(
                            x => x.InvLineNbr)
                        .ThenBy(
                            x => x.Gkey)
                        .ToList();

                if (lines.Count == 0)
                {
                    item.Outcome =
                        "Failed";

                    item.Message =
                        "Final invoice contains no invoice lines.";

                    response.FailedCount++;

                    continue;
                }

                // -------------------------------------------------
                // DRY RUN
                //
                // IMPORTANT:
                // Do NOT call StageInvoice here because that method
                // adds a GstGstr1Document to the EF context.
                // -------------------------------------------------

                if (request.DryRun)
                {
                    var preparation =
                        _stagingService.PrepareInvoice(
                            invoice,
                            lines);

                    item.Outcome =
                        "WouldStage";

                    item.Message =
                        $"{preparation.LineCount} line(s); " +
                        $"Category={preparation.ReturnCategory}; " +
                        $"Table={preparation.Gstr1Table ?? "-"}; " +
                        $"Reportable={preparation.IsReportable}.";

                    response.WouldStageCount++;

                    continue;
                }

                // -------------------------------------------------
                // EXECUTE
                // -------------------------------------------------

                // =========================================================
                // EXECUTE
                //
                // Each historical invoice gets its own transaction.
                // A failure on one invoice must not affect the others.
                // =========================================================

                await using var transaction =
                    await _unitOfWork.BeginTransactionAsync(
                        cancellationToken);

                try
                {
                    var stageResult =
                        _stagingService.StageInvoice(
                            invoice,
                            lines);

                    if (stageResult.Outcome ==
                        Gstr1StageOutcome.AlreadyStaged)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        item.Outcome =
                            "AlreadyStaged";

                        item.Message =
                            "Invoice was already present in GST staging.";

                        response.AlreadyStagedCount++;

                        continue;
                    }

                    await _unitOfWork.SaveChangesAsync(
                        cancellationToken);

                    await transaction.CommitAsync(
                        cancellationToken);

                    item.Outcome =
                        "Staged";

                    item.Message =
                        $"{lines.Count} invoice line(s) staged.";

                    response.StagedCount++;
                }
                catch
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    // Remove failed Added/Modified entities from the
                    // DbContext before processing the next invoice.
                    _unitOfWork.ClearChanges();

                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A failed SaveChanges may leave Added/Modified entities
                // tracked by the DbContext. Remove them before continuing
                // with the next historical invoice.
                _unitOfWork.ClearChanges();

                item.Outcome =
                    "Failed";

                item.Message =
                    ex.Message;

                response.FailedCount++;

                /*
                 * IMPORTANT:
                 *
                 * We do not throw here because one historical invoice
                 * must not terminate the entire period.
                 *
                 * See note below about EF tracking after a failed
                 * SaveChanges.
                 */
            }
        }

        return response;
    }

    private static void ValidateRequest(
        Gstr1BackfillRequest request)
    {
        if (string.IsNullOrWhiteSpace(
                request.SupplierGstin))
        {
            throw new ArgumentException(
                "Supplier GSTIN is required.",
                nameof(request.SupplierGstin));
        }

        if (request.FromDate == default)
        {
            throw new ArgumentException(
                "From date is required.",
                nameof(request.FromDate));
        }

        if (request.ToDate == default)
        {
            throw new ArgumentException(
                "To date is required.",
                nameof(request.ToDate));
        }

        if (request.FromDate.Date >
            request.ToDate.Date)
        {
            throw new ArgumentException(
                "From date cannot be later than To date.");
        }

        /*
         * Safety guard.
         *
         * This is a period backfill utility, not an unrestricted
         * all-history conversion process.
         */
        if ((request.ToDate.Date -
             request.FromDate.Date).TotalDays > 366)
        {
            throw new ArgumentException(
                "A single GST backfill request cannot exceed 366 days.");
        }
    }

    private static string NormalizeGstin(
        string gstin)
    {
        return gstin
            .Trim()
            .ToUpperInvariant();
    }
}