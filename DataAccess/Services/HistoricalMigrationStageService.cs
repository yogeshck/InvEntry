using DataAccess.Models;
using DataAccess.Repository;
using InvEntry.Contracts.Gst;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public sealed class HistoricalMigrationStageService : IHistoricalMigrationStageService
{
    private const string DocumentType = "SalesInvoice";
    private readonly MijmsContext _context;
    private readonly IHistoricalInvoiceAuditService _audit;
    private readonly IGstr1StagingService _staging;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHistoricalMigrationPreviewTokenStore _tokens;
    private readonly ILogger<HistoricalMigrationStageService> _logger;

    public HistoricalMigrationStageService(MijmsContext context, IHistoricalInvoiceAuditService audit,
        IGstr1StagingService staging, IUnitOfWork unitOfWork,
        IHistoricalMigrationPreviewTokenStore tokens, ILogger<HistoricalMigrationStageService> logger)
    {
        _context = context; _audit = audit; _staging = staging;
        _unitOfWork = unitOfWork; _tokens = tokens; _logger = logger;
    }

    public async Task<HistoricalMigrationStageResponse> StageAsync(string previewToken,
        string administratorIdentity, CancellationToken cancellationToken = default)
    {
        if (!_tokens.TryAcquire(previewToken, administratorIdentity, out var preview, out var error))
            throw new InvalidOperationException(error);

        var response = new HistoricalMigrationStageResponse
        {
            SupplierGstin = preview!.SupplierGstin, FromDate = preview.FromDate,
            ToDate = preview.ToDate, Previewed = preview.Candidates.Count,
            HistoricalInvoicesModified = 0
        };
        var completed = false;
        try
        {
            foreach (var candidate in preview.Candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = new HistoricalMigrationStageItem
                {
                    InvoiceNumber = candidate.InvoiceNumber, InvoiceDate = candidate.InvoiceDate
                };
                response.Items.Add(result);
                try
                {
                    // Re-read current evidence for every candidate immediately before its write attempt.
                    var current = await _audit.GetAsync(preview.SupplierGstin,
                        candidate.InvoiceDate.Date, candidate.InvoiceDate.Date, cancellationToken);
                    var matches = current.Items.Where(x =>
                        string.Equals(x.InvoiceNbr?.Trim(), candidate.InvoiceNumber.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        x.InvoiceDate?.Date == candidate.InvoiceDate.Date).ToList();
                    if (matches.Count != 1)
                    {
                        SourceChanged(response, result, "The source invoice could not be uniquely matched to the preview.");
                        continue;
                    }

                    var item = matches[0];
                    var assessment = HistoricalMigrationPreviewService.Assess(item);
                    if (assessment == "AlreadyStaged")
                    {
                        AlreadyStaged(response, result);
                        continue;
                    }
                    if (assessment != "QualifiedForStaging")
                    {
                        result.Result = "NOT QUALIFIED - SKIPPED";
                        result.Message = $"Current source assessment is no longer qualified ({assessment}).";
                        response.NoLongerQualified++;
                        continue;
                    }
                    if (!string.Equals(item.SourceFingerprint, candidate.SourceFingerprint, StringComparison.Ordinal))
                    {
                        SourceChanged(response, result, "Source evidence changed after preview.");
                        continue;
                    }

                    var header = await _context.InvoiceHeaders.AsNoTracking()
                        .SingleAsync(x => x.Gkey == item.InvoiceGkey, cancellationToken);
                    var lines = await _context.InvoiceLines.AsNoTracking()
                        .Where(x => x.InvoiceHdrGkey == item.InvoiceGkey)
                        .OrderBy(x => x.InvLineNbr).ThenBy(x => x.Gkey).ToListAsync(cancellationToken);
                    try { _staging.PrepareInvoice(header, lines); }
                    catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                    {
                        result.Result = "VALIDATION REQUIRED - SKIPPED";
                        result.Message = Safe(ex.Message);
                        response.ValidationRequired++;
                        continue;
                    }

                    await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        var duplicate = await _context.GstGstr1Documents.AsNoTracking().AnyAsync(x =>
                            x.SourceGkey == item.InvoiceGkey && x.DocumentType == DocumentType &&
                            x.SupplierGstin == preview.SupplierGstin, cancellationToken);
                        if (duplicate)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            AlreadyStaged(response, result);
                            continue;
                        }

                        var staged = _staging.StageInvoice(header, lines);
                        if (staged.Outcome == Gstr1StageOutcome.AlreadyStaged)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            AlreadyStaged(response, result);
                            continue;
                        }
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        _unitOfWork.ClearChanges();
                        result.Result = "STAGED";
                        result.Message = "GSTR-1 staging document created. Historical invoice unchanged.";
                        response.StagedSuccessfully++;
                        _logger.LogInformation("Historical GST migration staged {InvoiceNumber} by administrator {Administrator}; fingerprint {Fingerprint}",
                            candidate.InvoiceNumber, administratorIdentity, candidate.SourceFingerprint);
                    }
                    catch (DbUpdateException ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _unitOfWork.ClearChanges();
                        var nowExists = await _context.GstGstr1Documents.AsNoTracking().AnyAsync(x =>
                            x.SourceGkey == item.InvoiceGkey && x.DocumentType == DocumentType &&
                            x.SupplierGstin == preview.SupplierGstin, cancellationToken);
                        if (nowExists) AlreadyStaged(response, result);
                        else throw new InvalidOperationException("The staging record could not be saved.", ex);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _unitOfWork.ClearChanges();
                        throw;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _unitOfWork.ClearChanges();
                    result.Result = "ERROR - NOT STAGED";
                    result.Message = Safe(ex.Message);
                    response.Errors++;
                    _logger.LogError(ex, "Historical GST migration failed for {InvoiceNumber}", candidate.InvoiceNumber);
                }
            }
            completed = true;
            return response;
        }
        finally
        {
            if (completed) _tokens.Retire(previewToken); else _tokens.Release(previewToken);
        }
    }

    private static void AlreadyStaged(HistoricalMigrationStageResponse response, HistoricalMigrationStageItem item)
    { item.Result = "ALREADY STAGED"; item.Message = "An existing GSTR-1 staging record was found."; response.AlreadyStaged++; }
    private static void SourceChanged(HistoricalMigrationStageResponse response, HistoricalMigrationStageItem item, string message)
    { item.Result = "SOURCE CHANGED - SKIPPED"; item.Message = message; response.SourceChanged++; }
    private static string Safe(string message) => string.IsNullOrWhiteSpace(message) ? "The operation failed without modifying the historical invoice." : message;
}
