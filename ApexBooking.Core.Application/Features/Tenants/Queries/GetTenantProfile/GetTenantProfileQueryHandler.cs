using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Tenants.Queries.GetTenantProfile
{
    internal sealed class GetTenantProfileQueryHandler : IQueryHandler<GetTenantProfileQuery, TenantProfileDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetTenantProfileQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TenantProfileDto> Handle(GetTenantProfileQuery query, CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.Slug == query.Slug,
                includes: t => t.TenantProfile
            );

            if (tenant is null)
                throw new NotFoundException("Tenant not found.");

            return tenant.TenantProfile.ToProfileDto();
        }
    }
}