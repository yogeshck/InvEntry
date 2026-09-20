using InvEntry.Contracts.Gst.Export;

namespace DataAccess.Services;

public interface IGstr1JsonExportService
{
    Task<Gstr1GstnExportResponse> ExportAsync(
        string supplierGstin, string returnPeriod, CancellationToken cancellationToken = default);
}