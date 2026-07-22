using System.Linq.Expressions;

namespace ApexBooking.SharedKernel
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        public Expression<Func<T, bool>>? Criteria { get; protected set; }
        public List<Func<IQueryable<T>, IQueryable<T>>> Includes { get; } = new();

        protected void AddInclude(Func<IQueryable<T>, IQueryable<T>> includeExpression)
        {
            Includes.Add(includeExpression);
        }
    }
}