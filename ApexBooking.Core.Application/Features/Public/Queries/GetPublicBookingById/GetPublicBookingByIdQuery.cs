using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicBookingById
{
    public sealed record GetPublicBookingByIdQuery(string Slug, Guid BookingId)
        : IQuery<PublicBookingDto>;
}