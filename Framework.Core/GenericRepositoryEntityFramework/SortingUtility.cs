using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApexBooking.SharedKernel.Models;

namespace Framework.Core.GenericRepositoryEntityFramework
{
    public static class SortingUtility
    {
        internal static IQueryable<T> ApplySorting<T>(
            this IQueryable<T> source,
            IEnumerable<SortParam> sortParams)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sortParams == null || !sortParams.Any()) return source;

            IOrderedQueryable<T>? orderedQuery = null;

            foreach (var param in sortParams)
            {
                bool isDescending = param.SortOrderDescending ?? false;
                bool isFirstSort = orderedQuery == null;

                orderedQuery = OrderingHelper(
                    isFirstSort ? source : orderedQuery!,
                    param.OrderProperty,
                    isDescending,
                    !isFirstSort
                );
            }

            return orderedQuery ?? source;
        }

        private static IOrderedQueryable<T> OrderingHelper<T>(
            IQueryable<T> source,
            string propertyName,
            bool descending,
            bool isSubsequentSort)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            Expression property = parameter;

            // Supports nested properties like "Service.Name"
            foreach (var prop in propertyName.Split('.'))
            {
                property = Expression.PropertyOrField(property, prop);
            }

            var lambda = Expression.Lambda(property, parameter);

            // Determine method name: OrderBy, OrderByDescending, ThenBy, or ThenByDescending
            string methodName = isSubsequentSort ? "ThenBy" : "OrderBy";
            if (descending) methodName += "Descending";

            var resultExp = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] { typeof(T), property.Type }, // Critical: uses actual property type, not 'object'
                source.Expression,
                Expression.Quote(lambda)
            );

            return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(resultExp);
        }
    }
}