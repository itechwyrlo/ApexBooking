using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.ValidateCancellationToken
{
    public sealed record ValidateCancellationTokenQuery(string Token)
        : IQuery<BaseResponse<CancellationTokenValidationDto>>;
}
