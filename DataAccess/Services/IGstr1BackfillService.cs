using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IGstr1BackfillService
{
    Task<Gstr1BackfillResponse> BackfillAsync(
        Gstr1BackfillRequest request,
        CancellationToken cancellationToken = default);
}