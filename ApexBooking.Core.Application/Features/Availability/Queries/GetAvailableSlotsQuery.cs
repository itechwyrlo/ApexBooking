using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Availability.Queries
{
    public sealed record GetAvailableSlotsQuery(
         string Slug,
         Guid ServiceId,
         Guid? ResourceId,
         DateOnly Date
     ) : IQuery<BaseResponse<AvailableSlotsDto>>;
}