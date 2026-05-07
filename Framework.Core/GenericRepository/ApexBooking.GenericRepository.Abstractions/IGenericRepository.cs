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
   Task<TEntity?> GetAsync(ISpecification<TEntity> spec);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> GetAllAsync<TProperty>(Expression<Func<TEntity, TProperty>> include);

    Task<QueryResult<TEntity>> GetPageAsync(
        QueryObjectParams queryObjectParams,
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[] includes);

    IQueryable<TEntity> GetQuery(Expression<Func<TEntity, bool>>? predicate = null);
}
