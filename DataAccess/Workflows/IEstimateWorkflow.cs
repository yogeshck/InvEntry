using InvEntry.Contracts.Estimates;

namespace DataAccess.Workflows;

public interface IEstimateWorkflow
{
    Task<SaveEstimateResponse> SaveAsync(
        SaveEstimateRequest request,
        CancellationToken cancellationToken = default);
}
