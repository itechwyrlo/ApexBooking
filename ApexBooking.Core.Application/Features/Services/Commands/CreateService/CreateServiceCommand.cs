using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.service
{
    public sealed record CreateServiceCommand(
        string Name,
        string? Description,
        int DurationMinutes,
        decimal Price,
        string CurrencyCode,
        List<Guid> StaffIds,
        int BufferBeforeMinutes,
        int BufferAfterMinutes,
        int? MinAdvanceBookingHours,
        int? MaxAdvanceBookingDays
    ) : ICommand<ServiceDto>;
}