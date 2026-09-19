using InvEntry.Contracts.Gst;

namespace DataAccess.Services;

public interface IGstr1StagingEnrichmentService
{
    Task<Gstr1EnrichmentResponse> EnrichAsync(
        Gstr1EnrichmentRequest request,
        CancellationToken cancellationToken = default);
}