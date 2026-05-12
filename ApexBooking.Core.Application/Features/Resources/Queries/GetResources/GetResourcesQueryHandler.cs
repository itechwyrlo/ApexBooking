using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;
using ApexBooking.Core.Application.Resources.Mappings;

namespace ApexBooking.Core.Application.Features.Resources.Queries.GetResources
{
    internal sealed class GetResourcesQueryHandler
    : IQueryHandler<GetResourcesQuery, PagedResult<ResourceDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetResourcesQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<ResourceDto>> Handle(
            GetResourcesQuery query,
            CancellationToken cancellationToken)
        {
            var pagedResult = await _unitOfWork.ResourceRepository.GetPageAsync(query.param);
            var resources = pagedResult.data.Select(r => r.ToResourceDto()).ToList();
      
            return new PagedResult<ResourceDto>(resources, pagedResult.total);
        }
    }
}