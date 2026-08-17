using InvEntry.Contracts.CustomerOrders;

namespace DataAccess.Workflows;

public interface ICustomerOrderWorkflow
{
    Task<SaveCustomerOrderResponse> SaveAsync(
        SaveCustomerOrderRequest request,
        CancellationToken cancellationToken = default);
}