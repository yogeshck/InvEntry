using DataAccess.Models;

namespace DataAccess.Workflows;

public interface IOldMetalTransferPostingService
{
    Task PostAsync(
        StockTransferHeader header,
        CancellationToken cancellationToken = default);
}