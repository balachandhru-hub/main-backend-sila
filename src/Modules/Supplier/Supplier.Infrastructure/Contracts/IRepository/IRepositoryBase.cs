using System.Linq.Expressions;

namespace Supplier.Infrastructure.Contracts.IRepository;

public interface IRepositoryBase<T>
    where T : class
{
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression);

    T? FindFirstByCondition(Expression<Func<T, bool>> expression);

    Task<T?> FindFirstByConditionAsync(Expression<Func<T, bool>> expression);

    IQueryable<T> FindByConditionAsync(Expression<Func<T, bool>> expression);

    void Create(T entity);

    Task CreateAsync(T entity);

    void CreateRange(List<T> entities);

    Task CreateRangeAsync(List<T> entities);

    void Update(T entity);

    void UpdateRange(List<T> entities);

    void Delete(T entity);

    void DeleteRange(IEnumerable<T> entities);

    void DetachAllEntities();
}