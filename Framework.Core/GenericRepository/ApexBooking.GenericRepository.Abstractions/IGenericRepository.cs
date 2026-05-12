using System.Linq.Expressions;
using ApexBooking.SharedKernel;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.GenericRepository.Abstractions;

public interface IGenericRepository<TEntity> where TEntity : class, IAggregateRoot
{
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);

    Task<TEntity?> GetByIdAsync(object id);
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, params Expression<Func<TEntity, object>>[] includes);

    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> GetAllAsync<TProperty>(Expression<Func<TEntity, TProperty>> include);

    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);
    // In IGenericRepository.cs
    Task<IEnumerable<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>> filter = null,
        params Expression<Func<TEntity, object>>[] includes);


    Task<QueryResult<TEntity>> GetPageAsync(
        QueryObjectParams queryObjectParams,
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[] includes);
}
