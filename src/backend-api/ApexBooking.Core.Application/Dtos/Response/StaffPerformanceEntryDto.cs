namespace ApexBooking.Core.Application.Dtos.Response
{
    public record StaffPerformanceEntryDto(Guid TenantMemberId, string Name, int ServicesCompleted, decimal RevenueGenerated, string CurrencyCode);
}
