using ApexBooking.Core.Domain.Enums;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Dtos.Descriptor
{
    public record TokenDescriptor(
       Guid UserId,
       string Email,
       string FullName,
       bool IsPlatformAdmin,
       TenantId? TenantId = null,
       SystemRole? Role = null
   );

    public record TokenPrincipal(
        Guid UserId,
        string Email,
        string FullName,
        bool IsPlatformAdmin,
        TenantId? TenantId,
        SystemRole? Role
    );
}