using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Availability.Commands.SetResourceAvailability
{
    public class SetResourceAvailabilityHandler : ICommandHandler<SetResourceAvailabilityCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public SetResourceAvailabilityHandler(IUserContextService contextService, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task Handle(SetResourceAvailabilityCommand command, CancellationToken ct)
        {
            
            var resource = await _unitOfWork.ResourceRepository
                .FindByIdWithAvailabilityAsync(new ResourceId(command.ResourceId), ct);

            if (resource is null)
                throw new BusinessRuleBrokenException("Resource not found.");

            var scheduleEntities = new List<ResourceAvailabilitySchedule>();

            foreach (var dto in command.Schedules)
            {
                TimeOnly? startTime = dto.IsAvailable && !string.IsNullOrWhiteSpace(dto.StartTime)
                    ? TimeOnly.ParseExact(dto.StartTime, "HH:mm")
                    : null;

                TimeOnly? endTime = dto.IsAvailable && !string.IsNullOrWhiteSpace(dto.EndTime)
                    ? TimeOnly.ParseExact(dto.EndTime, "HH:mm")
                    : null;

                var schedule = ResourceAvailabilitySchedule.Create(
                    resource.ResourceId,
                    resource.TenantId,
                    (DayOfWeek)dto.DayOfWeek,
                    dto.IsAvailable,
                    startTime,
                    endTime
                );

                foreach (var brk in dto.Breaks)
                {
                    var breakStart = TimeOnly.ParseExact(brk.BreakStartTime, "HH:mm");
                    var breakEnd = TimeOnly.ParseExact(brk.BreakEndTime, "HH:mm");
                    schedule.AddBreakPeriod(breakStart, breakEnd, brk.Label);
                }

                scheduleEntities.Add(schedule);
            }

            resource.SetWeeklySchedule(scheduleEntities);

            _unitOfWork.ResourceRepository.Update(resource);
            await _unitOfWork.CompleteAsync(ct);
        }
    }
}