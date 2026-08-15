using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.BookableStaffs
{

    public record GetBookableStaffQuery(Guid BranchId, Guid ServiceId) : IQuery<IReadOnlyCollection<BookableStaffSummary>>;
}