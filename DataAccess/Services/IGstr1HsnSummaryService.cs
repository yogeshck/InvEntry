using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IGstr1HsnSummaryService
{
    Task<Gstr1HsnSummaryResponse> GetSummaryAsync(
        Gstr1HsnSummaryQuery query,
        CancellationToken cancellationToken = default);
}