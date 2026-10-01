using System.Linq.Expressions;

namespace Operations.Infrastructure.Contracts.IRepository
{
    /// <summary>
    /// Interface <c>IRepositoryBase</c> used to define the methods related for repository base.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRepositoryBase<T>
    {
        /// <summary>
        /// Finds the elements of type `T` that satisfy the condition (not tracked).
        /// </summary>
        IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression);

        /// <summary>
        /// Finds the first element that satisfies the condition (tracked).
        /// </summary>
        T FindFirstByCondition(Expression<Func<T, bool>> expression);

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        void Create(T entity);

        /// <summary>
        /// Updates the entity.
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// Deletes the specified entity from the repository.
        /// </summary>
        void Delete(T entity);

        /// <summary>
        /// Updates the range.
        /// </summary>
        void UpdateRange(List<T> entity);

        /// <summary>
        /// Adds the range.
        /// </summary>
        void CreateRange(List<T> entity);

        /// <summary>
        /// Finds the first element that satisfies the condition (tracked).
        /// </summary>
        Task<T> FindFirstByConditionAsync(Expression<Func<T, bool>> expression);

        /// <summary>
        /// Finds the elements of type `T` that satisfy the condition (not tracked).
        /// </summary>
        IQueryable<T> FindByConditionAsync(Expression<Func<T, bool>> expression);

        /// <summary>
        /// Adds the range.
        /// </summary>
        Task CreateRangeAsync(List<T> entity);

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        Task CreateAsync(T entity);

        /// <summary>
        /// Deletes the specified entities from the repository.
        /// </summary>
        void DeleteRange(IEnumerable<T> entities);

        /// <summary>
        /// Detaches every tracked entity.
        /// </summary>
        void DetachAllEntities();
    }
}
