using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Tenants.Queries.GetTenantProfile
{
    internal sealed class GetTenantProfileQueryHandler : IQueryHandler<GetTenantProfileQuery, BaseResponse<TenantProfileDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetTenantProfileQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<TenantProfileDto>> Handle(GetTenantProfileQuery query, CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.Slug == query.Slug,
                includes: t => t.TenantProfile
            );

            var profile = tenant.TenantProfile.ToProfileDto();

            return BaseResponse<TenantProfileDto>.Success(profile);
        }
    }
}