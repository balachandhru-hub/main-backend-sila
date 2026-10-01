using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Operations.Infrastructure.Repository
{
    /// <summary>
    /// Base repository shared by every entity repository.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RepositoryBase<T> : IRepositoryBase<T>
        where T : class
    {
        protected RepositoryContext RepositoryContext { get; set; }

        protected RepositoryBase(RepositoryContext repositoryContext)
        {
            RepositoryContext = repositoryContext;
        }

        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
        {
            return RepositoryContext.Set<T>().Where(expression).AsNoTracking();
        }

        public T FindFirstByCondition(Expression<Func<T, bool>> expression)
        {
            return RepositoryContext.Set<T>().Where(expression).FirstOrDefault()!;
        }

        public void Create(T entity)
        {
            _ = RepositoryContext.Set<T>().Add(entity);
        }

        public void Update(T entity)
        {
            _ = RepositoryContext.Set<T>().Update(entity);
        }

        public void Delete(T entity)
        {
            RepositoryContext.Set<T>().Remove(entity);
        }

        public void UpdateRange(List<T> entity)
        {
            RepositoryContext.Set<T>().UpdateRange(entity);
        }

        public void CreateRange(List<T> entity)
        {
            RepositoryContext.Set<T>().AddRange(entity);
        }

        public async Task<T> FindFirstByConditionAsync(Expression<Func<T, bool>> expression)
        {
            return (await RepositoryContext.Set<T>().Where(expression).FirstOrDefaultAsync())!;
        }

        public IQueryable<T> FindByConditionAsync(Expression<Func<T, bool>> expression)
        {
            return RepositoryContext.Set<T>().Where(expression).AsNoTracking();
        }

        public async Task CreateRangeAsync(List<T> entity)
        {
            await RepositoryContext.Set<T>().AddRangeAsync(entity);
        }

        public async Task CreateAsync(T entity)
        {
            _ = await RepositoryContext.Set<T>().AddAsync(entity);
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            RepositoryContext.Set<T>().RemoveRange(entities);
        }

        public void DetachAllEntities()
        {
            var trackedEntries = RepositoryContext.ChangeTracker.Entries().ToList();
            foreach (var entry in trackedEntries)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
