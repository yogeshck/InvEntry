using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DataAccess.Repository;

public class RepositoryBase<TEntity> :
    IRepositoryBase<TEntity>
    where TEntity : class
{
    protected readonly MijmsContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public RepositoryBase(MijmsContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public void Add(TEntity entity)
    {
        DbSet.Add(entity);
    }

    public void AddRange(IEnumerable<TEntity> entities)
    {
        DbSet.AddRange(entities);
    }

    public void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public void BulkUpdate(IEnumerable<TEntity> entities)
    {
        DbSet.UpdateRange(entities);
    }

    public void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public TEntity? GetId(int id)
    {
        return DbSet.Find(id);
    }

    public async Task<TEntity?> GetIdAsync(int id)
    {
        return await DbSet.FindAsync(id);
    }

    public TEntity? Get(
        Expression<Func<TEntity, bool>> predicate)
    {
        return DbSet.FirstOrDefault(predicate);
    }

    public async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.FirstOrDefaultAsync(predicate);
    }

    public IEnumerable<TEntity> GetList(
        Expression<Func<TEntity, bool>> predicate)
    {
        return DbSet
            .Where(predicate)
            .ToList();
    }

    public async Task<IEnumerable<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet
            .Where(predicate)
            .ToListAsync();
    }

    public IEnumerable<TEntity> GetAll()
    {
        return DbSet.ToList();
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public int Count()
    {
        return DbSet.Count();
    }

    public async Task<int> CountAsync()
    {
        return await DbSet.CountAsync();
    }

    public void Dispose()
    {
        // Intentionally empty.
        // DbContext lifetime is managed by DI.
    }
}