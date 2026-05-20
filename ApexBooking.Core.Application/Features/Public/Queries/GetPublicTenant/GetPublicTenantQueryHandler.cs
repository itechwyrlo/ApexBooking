using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicTenant
{
    internal sealed class GetPublicTenantQueryHandler : IQueryHandler<GetPublicTenantQuery, PublicTenantDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPublicTenantQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PublicTenantDto> Handle(GetPublicTenantQuery query, CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);

            if (tenant is null)
                throw new NotFoundException($"Tenant '{query.Slug}' not found.");

            return tenant.ToPublicTenantDto();
        }
    }
}
