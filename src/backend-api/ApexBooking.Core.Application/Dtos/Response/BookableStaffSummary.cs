using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos.Response
{
    public record BookableStaffSummary(
        Guid TenantMemberId,
        string FullName,
        string? CustomJobTitle,
        string? PhotoUrl
    );
}