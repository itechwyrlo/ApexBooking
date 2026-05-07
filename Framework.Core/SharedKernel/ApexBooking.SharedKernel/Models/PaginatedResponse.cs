using System.Collections.Generic;

namespace ApexBooking.SharedKernel.Models
{
    public sealed record PaginatedResponse<T>
    {
        public List<T> Data { get; init; }
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
        public bool HasNextPage { get; init; }
        public bool HasPreviousPage { get; init; }
    }
}
