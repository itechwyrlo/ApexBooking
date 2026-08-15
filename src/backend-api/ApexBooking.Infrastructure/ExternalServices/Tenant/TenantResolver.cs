using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Tenant;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Infrastructure.ExternalServices.Tenant;

public class TenantResolver : ITenantResolver
{
    private readonly IUnitOfWork _unitOfWork;

    public TenantResolver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantId?> ResolveBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.TenantRepository.GetAsync(
            predicate: t => t.Slug == slug);

        return tenant?.TenantId;
    }
}
