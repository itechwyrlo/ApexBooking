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
using ApexBooking.Core.Application.Services.Mappings;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetServiceById
{
    internal sealed class GetServiceByIdQueryHandler : IQueryHandler<GetServiceByIdQuery, BaseResponse<ServiceDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetServiceByIdQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<ServiceDto>> Handle(GetServiceByIdQuery query, CancellationToken ct)
        {
            var service = await _unitOfWork.ServiceRepository
                .GetAsync(
                    predicate:  s => s.ServiceId == new ServiceId(query.ServiceId),
                    includes: t => t.ServiceStaffs);
                    
            return BaseResponse<ServiceDto>.Success(service.ToServiceDto());
        }
    }
}