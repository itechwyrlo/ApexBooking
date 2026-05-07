using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicResources
{
   internal sealed class GetPublicResourcesQueryHandler
        : IQueryHandler<GetPublicResourcesQuery, BaseResponse<List<PublicResourceDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPublicResourcesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<List<PublicResourceDto>>> Handle(
            GetPublicResourcesQuery query,
            CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);

            if (tenant is null)
                return BaseResponse<List<PublicResourceDto>>.Failure($"Tenant '{query.Slug}' not found.");

            var serviceId = new ServiceId(query.ServiceId);

            var service = await _unitOfWork.ServiceRepository.FindByIdWithResourcesAsync(
                serviceId, cancellationToken);

            if (service is null || service.TenantId != tenant.TenantId || !service.IsActive)
                return BaseResponse<List<PublicResourceDto>>.Failure("Service not found.");

            var resourceIds = service.ServiceResources
                .Select(sr => sr.ResourceId)
                .ToList();

            var dtos = new List<PublicResourceDto>();

            foreach (var resourceId in resourceIds)
            {
                var resource = await _unitOfWork.ResourceRepository
                    .GetByIdAsync(resourceId);

                if (resource is null || !resource.IsActive)
                    continue;

                dtos.Add(new PublicResourceDto(
                    ResourceId: resource.ResourceId.Value,
                    Name: resource.Name,
                    Description: resource.Description
                ));
            }

            return BaseResponse<List<PublicResourceDto>>.Success(dtos);
        }
    }
}