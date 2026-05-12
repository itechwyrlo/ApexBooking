namespace ApexBooking.Core.Application.Dtos;

public sealed record ResourceAvailabilityExceptionDto(
    Guid Id,
    DateOnly ExceptionDate,
    string ExceptionType,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Note
);
