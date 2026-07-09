using System.Linq.Expressions;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    /// <summary>
    /// Interface <c>IRepositoryBase</c> used to define the methods related for repository base.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRepositoryBase<T>
    {

        /// <summary>
        /// FindByCondition(Expression<Func<T, bool>> expression);` method in the
        /// `IRepositoryBase` interface is used to find and return a collection of elements of type `T`
        /// that satisfy a specified condition.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression);

        /// <summary>
        /// This function finds and returns the first element in a collection that satisfies a specified
        /// condition.
        /// </summary>
        /// <param name="expression">The `expression` parameter in the `FindFirstByCondition` method is
        /// a lambda expression that represents a condition to filter the elements of type `T`. It is of
        /// type `Expression<Func<T, bool>>`, which means it is a lambda expression that takes an object
        /// of type `T` and</param>
        T FindFirstByCondition(Expression<Func<T, bool>> expression);

        /// <summary>
        /// The Create function in C# is used to create a new entity.
        /// </summary>
        /// <param name="entity">The "T" in the method signature `void Create(T entity);` represents a
        /// generic type parameter. This means that the method can work with any data type specified by
        /// the caller when the method is invoked. The actual type will be determined at compile time
        /// based on how the method is used.</param>
        void Create(T entity);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        void Update(T entity);

        /// <summary>
        /// Deletes the specified entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        void Delete(T entity);

        /// <summary>
        /// Updates the Range
        /// </summary>
        /// <param name="entity"></param>
        void UpdateRange(List<T> entity);

        /// <summary>
        /// Addes the Ranges
        /// </summary>
        /// <param name="entity"></param>
        void CreateRange(List<T> entity);

        /// <summary>
        /// Find The First By Condition
        /// </summary>
        /// <param name="entity"></param>
        Task<T> FindFirstByConditionAsync(Expression<Func<T, bool>> expression);

        /// <summary>
        /// This function finds and returns the first element in a collection that satisfies a specified
        /// condition.
        /// </summary>
        /// <param name="expression">The `expression` parameter in the `FindFirstByCondition` method is
        /// a lambda expression that represents a condition to filter the elements of type `T`. It is of
        /// type `Expression<Func<T, bool>>`, which means it is a lambda expression that takes an object
        /// of type `T` and</param>
        IQueryable<T> FindByConditionAsync(Expression<Func<T, bool>> expression);

        /// <summary>
        /// Addes the Ranges
        /// </summary>
        /// <param name="entity"></param>
        Task CreateRangeAsync(List<T> entity);
        /// <summary>
        ///
        /// </summary>
        /// <param name="entity"></param>
        Task CreateAsync(T entity);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        void DeleteRange(IEnumerable<T> entities);
        /// <summary>
        /// 
        /// </summary>
        void DetachAllEntities();
    }
}