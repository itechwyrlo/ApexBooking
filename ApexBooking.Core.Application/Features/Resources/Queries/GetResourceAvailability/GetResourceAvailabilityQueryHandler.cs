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

namespace ApexBooking.Core.Application.Features.Resources.Queries.GetResourceAvailability
{
    internal sealed class GetResourceAvailabilityQueryHandler
         : IQueryHandler<GetResourceAvailabilityQuery, BaseResponse<ResourceAvailabilityDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetResourceAvailabilityQueryHandler(
            IUnitOfWork unitOfWork,
            IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<ResourceAvailabilityDto>> Handle(
            GetResourceAvailabilityQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var resourceId = new ResourceId(query.ResourceId);

            var resource = await _unitOfWork.ResourceRepository
                .FindByIdWithAvailabilityAsync(resourceId, cancellationToken);

            if (resource is null || resource.TenantId != tenantId)
                return BaseResponse<ResourceAvailabilityDto>.Failure("Resource not found.");

            var schedules = resource.AvailabilitySchedules
                .OrderBy(s => s.DayOfWeek)
                .Select(s => new DayScheduleDto
                {
                    DayOfWeek = (int)s.DayOfWeek,
                    IsAvailable = s.IsAvailable,
                    StartTime = s.StartTime.HasValue
                        ? s.StartTime.Value.ToString("HH:mm")
                        : null,
                    EndTime = s.EndTime.HasValue
                        ? s.EndTime.Value.ToString("HH:mm")
                        : null,
                    Breaks = s.BreakPeriods
                        .Select(b => new BreakPeriodDto
                        {
                            BreakStartTime = b.BreakStartTime.ToString("HH:mm"),
                            BreakEndTime = b.BreakEndTime.ToString("HH:mm"),
                            Label = b.Label
                        })
                        .ToList()
                })
                .ToList();

            var dto = new ResourceAvailabilityDto
            {
                ResourceId = resource.ResourceId.Value,
                Schedules = schedules
            };

            return BaseResponse<ResourceAvailabilityDto>.Success(dto);
        }
    }
}