using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetServiceStaff
{
    public record StaffAssignmentSummary(
        Guid TenantMemberId,
        string FullName,
        string? CustomJobTitle,
        bool IsAssigned
    );

    public record GetServiceStaffQuery(Guid ServiceId) : IQuery<IReadOnlyCollection<StaffAssignmentSummary>>;
}
