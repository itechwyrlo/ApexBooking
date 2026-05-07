using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetServices
{
    internal sealed class GetServicesQueryHandler : IQueryHandler<GetServicesQuery, BaseResponse<List<ServiceDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetServicesQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<List<ServiceDto>>> Handle(GetServicesQuery query, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();

            var services = await _unitOfWork.ServiceRepository
                .GetActiveServicesAsync()
                .ConfigureAwait(false);

            var dtos = services
                .Where(s => s.TenantId == tenantId)
                .Select(s => new ServiceDto
                {
                    Id = s.ServiceId.Value,
                    Name = s.Name,
                    Description = s.Description,
                    DurationMinutes = s.DurationMinutes,
                    BufferBeforeMinutes = s.BufferBeforeMinutes,
                    BufferAfterMinutes = s.BufferAfterMinutes,
                    Price = s.Price,
                    CurrencyCode = s.CurrencyCode,
                    MinAdvanceBookingHours = s.MinAdvanceBookingHours,
                    MaxAdvanceBookingDays = s.MaxAdvanceBookingDays,
                    IsActive = s.IsActive,
                    ResourceIds = s.ServiceResources.Select(sr => sr.ResourceId.Value).ToList(),
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList();

            return BaseResponse<List<ServiceDto>>.Success(dtos);
        }
    }
}