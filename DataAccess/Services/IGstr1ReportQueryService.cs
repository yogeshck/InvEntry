using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IGstr1ReportQueryService
{
    Task<Gstr1ReturnSummaryResponse> GetSummaryAsync(
        string supplierGstin, string returnPeriod,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Gstr1DocumentResponse>> GetDocumentsAsync(
        string supplierGstin, string returnPeriod,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Gstr1DocumentLineResponse>?> GetDocumentLinesAsync(
        long documentGkey, string supplierGstin, string returnPeriod,
        CancellationToken cancellationToken = default);
}
