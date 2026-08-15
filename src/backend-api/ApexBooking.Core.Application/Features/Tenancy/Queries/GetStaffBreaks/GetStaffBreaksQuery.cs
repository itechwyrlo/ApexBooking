using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetStaffBreaks
{
    public record GetStaffBreaksQuery(Guid TenantMemberId) : IQuery<IReadOnlyCollection<StaffBreakSummary>>;
}
