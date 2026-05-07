using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApexBooking.GenericRepository.Abstractions;
using ApexBooking.SharedKernel;
using ApexBooking.SharedKernel.Models;
using Framework.Core.GenericRepositoryEntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.GenericRepository.EntityFramework
{
    public class GenericRepository<TEntity>(DbContext context) : IGenericRepository<TEntity>
    where TEntity : class, IAggregateRoot
    {

        protected readonly DbContext Context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

        public virtual void Add(TEntity entity) => _dbSet.Add(entity);
        public virtual void Update(TEntity entity) => _dbSet.Update(entity);
        public virtual void Remove(TEntity entity) => _dbSet.Remove(entity);

        public async Task<TEntity?> GetByIdAsync(object id) => await _dbSet.FindAsync(id).ConfigureAwait(false);


        public virtual async Task<TEntity?> GetAsync(ISpecification<TEntity> spec)
        {
            IQueryable<TEntity> query = _dbSet;

            // Apply filtering
            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            // Apply all type-safe Includes/ThenIncludes
            query = spec.Includes.Aggregate(query, (current, include) => include(current));

            return await query.FirstOrDefaultAsync().ConfigureAwait(false);
        }


        public async Task<IEnumerable<TEntity>> GetAllAsync() => await _dbSet.AsNoTracking().ToListAsync().ConfigureAwait(false);

        public async Task<IEnumerable<TEntity>> GetAllAsync<TProperty>(Expression<Func<TEntity, TProperty>> include)
        {
            return await _dbSet.Include(include).AsNoTracking().ToListAsync().ConfigureAwait(false);
        }

        public virtual async Task<QueryResult<TEntity>> GetPageAsync(
        QueryObjectParams queryObjectParams,
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            if (includes is { Length: > 0 })
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }

            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync().ConfigureAwait(false);

            if (queryObjectParams.SortingParams is { Count: > 0 })
            {
                query = SortingUtility.ApplySorting(query, queryObjectParams.SortingParams)!;
            }

            var items = await query
                .Skip((queryObjectParams.PageNumber - 1) * queryObjectParams.PageSize)
                .Take(queryObjectParams.PageSize)
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);

            return new QueryResult<TEntity>(items, totalCount);
        }

        public IQueryable<TEntity> GetQuery(Expression<Func<TEntity, bool>>? predicate = null)
        {
            IQueryable<TEntity> query = _dbSet;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query;
        }

    }
}