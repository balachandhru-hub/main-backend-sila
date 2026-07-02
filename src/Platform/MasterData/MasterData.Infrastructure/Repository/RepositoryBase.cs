using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MasterData.Application.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public abstract class RepositoryBase<T> : IRepositoryBase<T>
    where T : class
{
    protected RepositoryContext RepositoryContext { get; }

    protected RepositoryBase(RepositoryContext repositoryContext)
    {
        RepositoryContext = repositoryContext;
    }

    public IQueryable<T> FindByCondition(
        Expression<Func<T, bool>> expression)
    {
        return RepositoryContext.Set<T>()
            .Where(expression)
            .AsNoTracking();
    }

    public T? FindFirstByCondition(
        Expression<Func<T, bool>> expression)
    {
        return RepositoryContext.Set<T>()
            .Where(expression)
            .FirstOrDefault();
    }

    public async Task<T?> FindFirstByConditionAsync(
        Expression<Func<T, bool>> expression)
    {
        return await RepositoryContext.Set<T>()
            .Where(expression)
            .FirstOrDefaultAsync();
    }

    public IQueryable<T> FindByConditionAsync(
        Expression<Func<T, bool>> expression)
    {
        return RepositoryContext.Set<T>()
            .Where(expression)
            .AsNoTracking();
    }

    public void Create(T entity)
    {
        RepositoryContext.Set<T>().Add(entity);
    }

    public async Task CreateAsync(T entity)
    {
        await RepositoryContext.Set<T>().AddAsync(entity);
    }

    public void CreateRange(List<T> entities)
    {
        RepositoryContext.Set<T>().AddRange(entities);
    }

    public async Task CreateRangeAsync(List<T> entities)
    {
        await RepositoryContext.Set<T>().AddRangeAsync(entities);
    }

    public void Update(T entity)
    {
        RepositoryContext.Set<T>().Update(entity);
    }

    public void UpdateRange(List<T> entities)
    {
        RepositoryContext.Set<T>().UpdateRange(entities);
    }

    public void Delete(T entity)
    {
        RepositoryContext.Set<T>().Remove(entity);
    }

    public void DeleteRange(IEnumerable<T> entities)
    {
        RepositoryContext.Set<T>().RemoveRange(entities);
    }

    public void DetachAllEntities()
    {
        var trackedEntries = RepositoryContext.ChangeTracker
            .Entries()
            .ToList();

        foreach (var entry in trackedEntries)
        {
            entry.State = EntityState.Detached;
        }
    }
}