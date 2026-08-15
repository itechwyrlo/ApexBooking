namespace ApexBooking.Core.Application.Dtos.Response
{
    public record IdleStaffDto(Guid TenantMemberId, string Name, string? PhotoUrl);
}
