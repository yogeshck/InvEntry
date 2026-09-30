using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IHistoricalInvoiceAuditService
{
    Task<HistoricalInvoiceAuditResponse> GetAsync(
        string supplierGstin,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}
