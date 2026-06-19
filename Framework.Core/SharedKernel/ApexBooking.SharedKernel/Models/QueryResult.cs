using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.SharedKernel.Models
{
    public record QueryResult<T>
    {
        public IEnumerable<T> data { get; protected set; }
        public int total { get; protected set; }

        public QueryResult(IEnumerable<T> entities, int totalCount)
        {
            total = totalCount;

            data = entities;
        }
    }
}