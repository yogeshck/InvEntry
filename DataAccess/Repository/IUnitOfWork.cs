using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess.Repository;

public interface IUnitOfWork
{
    int SaveChanges();

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);
}