using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.ValidateCancellationToken
{
    public sealed record ValidateCancellationTokenQuery(string Token)
        : IQuery<CancellationTokenValidationDto>;
}
