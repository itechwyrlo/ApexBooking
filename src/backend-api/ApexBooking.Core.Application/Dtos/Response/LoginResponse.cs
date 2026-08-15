namespace ApexBooking.Core.Application.Dtos.Response
{
    public record LoginResponse(
        string Token,
        string Email,
        string FullName,
        bool IsPlatformAdmin,
        Guid? TenantId = null,
        string? TenantRole = null);
}