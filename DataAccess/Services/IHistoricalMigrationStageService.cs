using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IHistoricalMigrationStageService
{
    Task<HistoricalMigrationStageResponse> StageAsync(string previewToken,
        string administratorIdentity, CancellationToken cancellationToken = default);
}
