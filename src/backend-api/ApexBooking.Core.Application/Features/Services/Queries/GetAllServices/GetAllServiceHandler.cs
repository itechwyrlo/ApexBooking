using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetAllServices
{

     public class GetAllServiceHandler
        : IQueryHandler<GetAllServiceQuery, QueryResult<ServiceCatalogSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITenantEntity _tenantEntity;

        public GetAllServiceHandler(IUnitOfWork unitOfWork, IMapper mapper, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tenantEntity = tenantEntity;
        }

        public async Task<QueryResult<ServiceCatalogSummary>> Handle(
            GetAllServiceQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load service catalog. No authenticated tenant context was found.");

            var pagedResult = await _unitOfWork.TenantRepository.GetPageAsync(
                query.param,
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Services
            );

            var orderedServices = pagedResult.data
                .SelectMany(t => t.Services)
                .OrderBy(s => s.CreatedAt)
                .ToList();

            var mappedItems = _mapper.Map<IEnumerable<ServiceCatalogSummary>>(orderedServices);
            return new QueryResult<ServiceCatalogSummary>(mappedItems, orderedServices.Count);
        }
    }
}