using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetTeamMemberSchedule
{
    public record GetTeamMemberScheduleQuery(Guid TenantMemberId) : IQuery<IReadOnlyCollection<DayScheduleSummary>>;
}
