using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicBookingById
{
    public sealed record GetPublicBookingByIdQuery(string Slug, Guid BookingId) 
        : IQuery<BaseResponse<PublicBookingDto>>;
}