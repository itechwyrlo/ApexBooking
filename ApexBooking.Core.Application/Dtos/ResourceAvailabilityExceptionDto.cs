using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed class ResourceAvailabilityExceptionDto
    {
        public Guid Id { get; init; }
        public DateOnly ExceptionDate { get; init; }
        public string ExceptionType { get; init; } = string.Empty;
        public TimeOnly? StartTime { get; init; }
        public TimeOnly? EndTime { get; init; }
        public string? Note { get; init; }
    }
}