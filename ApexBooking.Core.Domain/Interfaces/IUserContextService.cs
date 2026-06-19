using System;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Interfaces
{
    public interface IUserContextService
    {
        string GetCurrentJti();
        string GetUserRole();
        Guid GetCurrentUserId();
        bool IsAuthenticated();
        TenantId GetCurrentTenantId();
    }
}
