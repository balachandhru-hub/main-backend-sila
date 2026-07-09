using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Supplier.Infrastructure.Repository
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RepositoryBase<T> : IRepositoryBase<T>
        where T : class
    {
        /// <summary>
        ///
        /// </summary>
        protected RepositoryContext RepositoryContext { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="repositoryContext"></param>
        protected RepositoryBase(RepositoryContext repositoryContext)
        {
            RepositoryContext = repositoryContext;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
        {
            return RepositoryContext.Set<T>().Where(expression).AsNoTracking();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public T FindFirstByCondition(Expression<Func<T, bool>> expression)
        {
            return RepositoryContext.Set<T>().Where(expression).FirstOrDefault()!;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        public void Create(T entity)
        {
            _ = RepositoryContext.Set<T>().Add(entity);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        public void Update(T entity)
        {
            _ = RepositoryContext.Set<T>().Update(entity);
        }

        /// <summary>
        /// Deletes the specified entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        public void Delete(T entity)
        {
            RepositoryContext.Set<T>().Remove(entity);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        public void UpdateRange(List<T> entity)
        {
            RepositoryContext.Set<T>().UpdateRange(entity);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        public void CreateRange(List<T> entity)
        {
            RepositoryContext.Set<T>().AddRange(entity);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public async Task<T> FindFirstByConditionAsync(Expression<Func<T, bool>> expression)
        {
            return await RepositoryContext.Set<T>().Where(expression).FirstOrDefaultAsync();
        }

        /// <summary>
        /// This function finds and returns the first element in a collection that satisfies a specified
        /// condition.
        /// </summary>
        /// <param name="expression">The `expression` parameter in the `FindFirstByCondition` method is
        /// a lambda expression that represents a condition to filter the elements of type `T`. It is of
        /// type `Expression<Func<T, bool>>`, which means it is a lambda expression that takes an object
        /// of type `T` and</param>
        public IQueryable<T> FindByConditionAsync(Expression<Func<T, bool>> expression)
        {
            return RepositoryContext.Set<T>().Where(expression).AsNoTracking();
        }


        /// <summary>
        /// Addes the Ranges
        /// </summary>
        /// <param name="entity"></param>
        public async Task CreateRangeAsync(List<T> entity)
        {
            await RepositoryContext.Set<T>().AddRangeAsync(entity);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
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
