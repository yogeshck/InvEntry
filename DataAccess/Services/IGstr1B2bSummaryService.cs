using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IGstr1B2bSummaryService
{
    Task<Gstr1B2bSummaryResponse> GetSummaryAsync(
        Gstr1B2bSummaryQuery query,
        CancellationToken cancellationToken = default);
}
