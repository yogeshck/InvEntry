using InvEntry.Contracts.Gst.Export;

namespace DataAccess.Services;

public sealed class Gstr1JsonExportService(
    IGstr1ExportPreparationService preparationService) : IGstr1JsonExportService
{
    public async Task<Gstr1GstnExportResponse> ExportAsync(
        string supplierGstin, string returnPeriod, CancellationToken cancellationToken = default)
    {
        // Reject invalid periods before starting the existing sequential database pipeline.
        Gstr1GstnExportMapper.ToFilingPeriod(returnPeriod);
        var prepared = await preparationService.PrepareAsync(
            supplierGstin, returnPeriod, cancellationToken);
        return Gstr1GstnExportMapper.Map(prepared);
    }
}