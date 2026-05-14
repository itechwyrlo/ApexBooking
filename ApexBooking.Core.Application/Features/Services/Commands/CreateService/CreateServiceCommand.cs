using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.SharedKernel.Models;

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
    ) : ICommand<BaseResponse<ServiceDto>>;
}