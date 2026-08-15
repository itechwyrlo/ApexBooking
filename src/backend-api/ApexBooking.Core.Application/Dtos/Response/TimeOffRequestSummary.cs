using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos.Response
{
    public record TimeOffRequestSummary(
        Guid RequestId,
        Guid TenantMemberId,
        string MemberName,
        string? MemberPhotoUrl,
        TimeOffType Type,
        DateOnly StartDate,
        DateOnly EndDate,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        TimeOffStatus Status,
        string? Reason,
        DateTime RequestedAt,
        DateTime? DecidedAt
    );
}
