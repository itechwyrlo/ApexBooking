using System;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Availability.Queries
{
    public sealed record GetAvailableSlotsQuery(
        string Slug,
        Guid ServiceId,
        Guid? StaffId,
        DateOnly Date
    ) : IQuery<AvailableSlotsDto>;
}