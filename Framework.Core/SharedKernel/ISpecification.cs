using System.Linq.Expressions;

namespace ApexBooking.SharedKernel
{
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>>? Criteria { get; }
        // This allows chaining .Include().ThenInclude()
        List<Func<IQueryable<T>, IQueryable<T>>> Includes { get; }
    }
}