using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services.Auth
{
    public interface ITokenService
    {
        string GenerateAccessToken(TokenDescriptor descriptor);

        TokenPrincipal? ValidateExpiredAccessToken(string accessToken);
    }

    public sealed record TokenDescriptor(
    Guid UserId,
    string Email,
    string FullName,
    bool IsPlatformAdmin,
    TenantId? TenantId,
    SystemRole? Role,
    string? Slug = null,
    TenantMemberId? TenantMemberId = null);

    public sealed record TokenPrincipal(
    Guid UserId,
    string Email,
    string FullName,
    bool IsPlatformAdmin,
    TenantId? TenantId,
    SystemRole? Role,
    string? Slug = null,
    TenantMemberId? TenantMemberId = null);
}