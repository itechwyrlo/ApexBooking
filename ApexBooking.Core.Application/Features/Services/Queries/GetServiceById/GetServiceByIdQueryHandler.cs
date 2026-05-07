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
            var tenantId = _contextService.GetCurrentTenantId();
            var serviceId = new ServiceId(query.ServiceId);

            var service = await _unitOfWork.ServiceRepository
                .FindByIdWithResourcesAsync(serviceId, ct)
                .ConfigureAwait(false);

            if (service is null || service.TenantId != tenantId)
                return BaseResponse<ServiceDto>.Failure("Error, Service not found!" , code: "404");

            return BaseResponse<ServiceDto>.Success(new ServiceDto
            {
                Id = service.ServiceId.Value,
                Name = service.Name,
                Description = service.Description,
                DurationMinutes = service.DurationMinutes,
                BufferBeforeMinutes = service.BufferBeforeMinutes,
                BufferAfterMinutes = service.BufferAfterMinutes,
                Price = service.Price,
                CurrencyCode = service.CurrencyCode,
                MinAdvanceBookingHours = service.MinAdvanceBookingHours,
                MaxAdvanceBookingDays = service.MaxAdvanceBookingDays,
                IsActive = service.IsActive,
                ResourceIds = service.ServiceResources.Select(sr => sr.ResourceId.Value).ToList(),
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt
            });
        }
    }
}