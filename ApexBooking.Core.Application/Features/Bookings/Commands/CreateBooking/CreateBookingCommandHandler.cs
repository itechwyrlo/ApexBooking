using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Slot;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CreateBooking
{
    internal sealed class CreateBookingCommandHandler : ICommandHandler<CreateBookingCommand, BaseResponse<BookingDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContextService;
        private readonly SlotAvailabilityService _slotAvailabilityService;

        public CreateBookingCommandHandler(
            IUnitOfWork unitOfWork,
            IUserContextService userContextService,
            SlotAvailabilityService slotAvailabilityService)
        {
            _unitOfWork = unitOfWork;
            _userContextService = userContextService;
            _slotAvailabilityService = slotAvailabilityService;
        }

        public async Task<BaseResponse<BookingDto>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _userContextService.GetCurrentTenantId();
            var userId = _userContextService.GetCurrentUserId();

            var serviceId = new ServiceId(request.ServiceId);
            var resourceId = new ResourceId(request.ResourceId);

            var service = await _unitOfWork.ServiceRepository.FindByIdWithResourcesAsync(serviceId, cancellationToken);
            if (service is null || !service.IsActive)
                return BaseResponse<BookingDto>.Failure("Service not found");

            if (!service.IsResourceAssigned(resourceId))
                return BaseResponse<BookingDto>.Failure("Resource not assigned to service");

            var resource = await _unitOfWork.ResourceRepository.FindByIdWithAvailabilityAsync(resourceId, cancellationToken);
            if (resource is null || !resource.IsActive)
                return BaseResponse<BookingDto>.Failure("Resource not found");

            var tenant = await _unitOfWork.TenantRepository.GetTenantSettingsById(tenantId);
            if (tenant is null)
                return BaseResponse<BookingDto>.Failure("Tenant not found");

            var settings = tenant.TenantSettings;
            if (settings is null)
                return BaseResponse<BookingDto>.Failure("Tenant settings not found");

            var minAdvanceHours = service.GetEffectiveMinAdvanceBookingHours(settings.MinAdvanceBookingHours);
            var maxAdvanceDays = service.GetEffectiveMaxAdvanceBookingDays(settings.MaxAdvanceBookingDays);

            var now = DateTime.UtcNow;
            var scheduledDateTime = request.ScheduledDate.ToDateTime(request.ScheduledStartTime);

            if (scheduledDateTime < now.AddHours(minAdvanceHours))
                return BaseResponse<BookingDto>.Failure("Booking is too soon");

            if (scheduledDateTime > now.AddDays(maxAdvanceDays))
                return BaseResponse<BookingDto>.Failure("Booking is too far in advance");

            var activeBookings = await _unitOfWork.BookingRepository
                .GetActiveBookingsForResourceOnDateAsync(resourceId, request.ScheduledDate, cancellationToken);

            var availableSlots = _slotAvailabilityService.ComputeAvailableSlots(
                service,
                resource,
                request.ScheduledDate,
                activeBookings);

            var requestedSlot = request.ScheduledStartTime.ToString("HH:mm");

            if (!availableSlots.Contains(requestedSlot))
                return BaseResponse<BookingDto>.Failure("Selected slot is no longer available");

            var year = request.ScheduledDate.Year;

            var existingBookings = _unitOfWork.BookingRepository.GetQuery(b =>
                b.TenantId == tenantId &&
                b.ScheduledDate.Year == year);

            var count = existingBookings.Count();
            var sequence = count + 1;

            var bookingReference = $"BK-{year}-{sequence.ToString("D5")}";

            var booking = Booking.Create(
                tenantId,
                bookingReference,
                serviceId,
                resourceId,
                userId,
                request.ScheduledDate,
                request.ScheduledStartTime,
                service.DurationMinutes,
                settings.BookingConfirmationMode,
                service.Price,
                service.CurrencyCode,
                request.CustomerNotes);

            _unitOfWork.BookingRepository.Add(booking);

            await _unitOfWork.CompleteAsync(cancellationToken);

            var dto = new BookingDto(
                booking.BookingId.Value,
                booking.BookingReference,
                booking.ServiceId.Value,
                service.Name,
                booking.ResourceId.Value,
                resource.Name,
                booking.UserId,
                booking.ScheduledDate,
                booking.ScheduledStartTime,
                booking.ScheduledEndTime,
                booking.DurationMinutes,
                booking.Status,
                booking.ConfirmationMode,
                booking.PriceSnapshot,
                booking.CurrencyCode,
                booking.CustomerNotes,
                booking.CancellationReason,
                booking.CancelledAt,
                booking.CreatedAt);

            return BaseResponse<BookingDto>.Success(dto);
        }
    }
}