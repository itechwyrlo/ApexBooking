using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Interfaces
{
    public interface IUserContextService
    {
        string GetUserRole();
        Guid GetCurrentUserId();
        bool IsAuthenticated();
        bool IsPlatformAdmin();
        TenantId GetCurrentTenantId();
    }
}
