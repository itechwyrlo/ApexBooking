using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.SharedKernel.Utils
{

    public static class PaginationUtility
    {
        public static IQueryable<T> Execute<T>(this IQueryable<T> query, PageParam param, string sortBy, string? sortDirection = null)
        {
            if (param is null) return query;
            var paramExp = Expression.Parameter(typeof(T), "x");
            var propertyExp = Expression.Property(paramExp, sortBy);
            var propertyExpToObject = Expression.Convert(propertyExp, typeof(object));
            var sortExp = Expression.Lambda<Func<T, object>>(propertyExpToObject, paramExp);

            if (!string.IsNullOrEmpty(sortDirection) && sortDirection.ToLower() == "asc")
            {
                return query.OrderBy(sortExp).GetPage(param);
            }

            return query.OrderByDescending(sortExp).GetPage(param);

        }

        private static IQueryable<T> GetPage<T>(this IQueryable<T> query, PageParam param)
        {
            if (param.PageNumber > 0 || param.PageSize > 0)
            {
                return query.Skip((param.PageNumber - 1) * param.PageSize).Take(param.PageSize);
            }

            return query;
        }
    }


}