using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicTenant
{
    internal sealed class GetPublicTenantQueryHandler
    : IQueryHandler<GetPublicTenantQuery, BaseResponse<PublicTenantDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPublicTenantQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<PublicTenantDto>> Handle(
            GetPublicTenantQuery query,
            CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);

            if (tenant is null)
                return BaseResponse<PublicTenantDto>.Failure($"Tenant '{query.Slug}' not found.");

            return BaseResponse<PublicTenantDto>.Success(tenant.ToPublicTenantDto());
        }
    }
}
