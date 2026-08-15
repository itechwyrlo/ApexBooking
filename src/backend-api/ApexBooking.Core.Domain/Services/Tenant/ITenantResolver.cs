using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services.Tenant
{
    public interface ITenantResolver
    {
        Task<TenantId?> ResolveBySlugAsync(string slug, CancellationToken cancellationToken = default);
    }
}
