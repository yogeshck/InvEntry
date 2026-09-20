using InvEntry.Contracts.Gst.Export;

namespace DataAccess.Services;

public interface IGstr1ExportPreparationService
{
    Task<Gstr1ExportPreparationResponse> PrepareAsync(
        string supplierGstin,
        string returnPeriod,
        CancellationToken cancellationToken = default);
}
