namespace ApexBooking.Core.Application.Dtos.Request
{
    public record PendingTenantRequestSummary(
        Guid TenantRegistrationRequestId,
        string BusinessName,
        string BusinessType,
        string RequestedSlug,
        string RequestedPlan,
        string OwnerFirstName,
        string OwnerLastName,
        string OwnerEmail,
        string Status,
        DateTime RequestedAt);
}