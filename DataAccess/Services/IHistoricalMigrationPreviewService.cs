using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IHistoricalMigrationPreviewService
{
    Task<HistoricalMigrationPreviewResponse> PreviewAsync(
        HistoricalMigrationPreviewRequest request,
        string administratorIdentity,
        CancellationToken cancellationToken = default);
}
