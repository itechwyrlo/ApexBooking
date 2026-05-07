using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Resources.Queries.GetResources
{
    internal sealed class GetResourcesQueryHandler
    : IQueryHandler<GetResourcesQuery, BaseResponse<List<ResourceDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetResourcesQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<List<ResourceDto>>> Handle(
            GetResourcesQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();

            var result = await _unitOfWork.ResourceRepository
                .GetPageAsync(
                    new QueryObjectParams { PageNumber = 1, PageSize = int.MaxValue },
                    r => r.TenantId == tenantId && r.IsActive)
                .ConfigureAwait(false);

            var dtos = result.data.Select(r => new ResourceDto
            {
                Id = r.ResourceId.Value,
                Name = r.Name,
                Description = r.Description,
                ResourceType = r.ResourceType.ToString(),
                Capacity = r.Capacity,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();

            return BaseResponse<List<ResourceDto>>.Success(dtos);
        }
    }
}