using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Services.Mappings;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetServices
{
    internal sealed class GetServicesQueryHandler
    : IQueryHandler<GetServicesQuery, PagedResult<ServiceDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetServicesQueryHandler(IUserContextService contextService, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<PagedResult<ServiceDto>> Handle(
            GetServicesQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();

            var pagedResult = await _unitOfWork.ServiceRepository.GetPageAsync(
                query.param,
                predicate: s => s.TenantId == tenantId,
                includes: s => s.ServiceStaffs)
                .ConfigureAwait(false);

            var dtos = pagedResult.data
                .Select(b => b.ToServiceDto())
                .ToList();



            return new PagedResult<ServiceDto>(dtos, pagedResult.total);
        }
    }
}