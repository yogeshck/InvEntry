using DataAccess.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess.Repository;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MijmsContext _context;

    public UnitOfWork(MijmsContext context)
    {
        _context = context;
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(
            cancellationToken);
    }

    public Task<IDbContextTransaction>
        BeginTransactionAsync(
            CancellationToken cancellationToken = default)
    {
        return _context.Database
            .BeginTransactionAsync(cancellationToken);
    }
}