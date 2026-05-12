using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.Core.Application.Resources.Mappings;

namespace ApexBooking.Core.Application.Features.Resources.Queries.GetResourceById
{
    internal sealed class GetResourceByIdQueryHandler
     : IQueryHandler<GetResourceByIdQuery, BaseResponse<ResourceDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetResourceByIdQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<ResourceDto>> Handle(
            GetResourceByIdQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var resourceId = new ResourceId(query.ResourceId);

            var resource = await _unitOfWork.ResourceRepository
                .GetByIdAsync(resourceId)
                .ConfigureAwait(false);

            if (resource is null || resource.TenantId != tenantId)
                return BaseResponse<ResourceDto>.Failure("Resource not found.");

            

            return BaseResponse<ResourceDto>.Success(resource.ToResourceDto());
        }
    }
}