using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IGstr1B2csSummaryService
{
    Task<Gstr1B2csSummaryResponse> GetSummaryAsync(
        Gstr1B2csSummaryQuery query,
        CancellationToken cancellationToken = default);
}
