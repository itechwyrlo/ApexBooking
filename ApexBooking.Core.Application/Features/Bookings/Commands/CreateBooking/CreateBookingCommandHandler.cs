using ApexBooking.Core.Application.Common;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.Core.Domain.Services.Slot;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CreateBooking
{
    internal sealed class CreateBookingCommandHandler : ICommandHandler<CreateBookingCommand, BaseResponse<BookingDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SlotAvailabilityService _slotAvailabilityService;
        private readonly INotificationService _notificationService;
        private readonly ITokenService _tokenService;
        private readonly IAppUrlService _appUrlService;

        public CreateBookingCommandHandler(
            IUnitOfWork unitOfWork,
            SlotAvailabilityService slotAvailabilityService,
            INotificationService notificationService,
            ITokenService tokenService,
            IAppUrlService appUrlService)
        {
            _unitOfWork = unitOfWork;
            _slotAvailabilityService = slotAvailabilityService;
            _notificationService = notificationService;
            _tokenService = tokenService;
            _appUrlService = appUrlService;
        }

        public async Task<BaseResponse<BookingDetailDto>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(request.TenantSlug);

            if (tenant is null)
                return BaseResponse<BookingDetailDto>.Failure("Tenant not found.");

            var settings = tenant.TenantSettings;
            if (settings is null)
                return BaseResponse<BookingDetailDto>.Failure("Tenant settings not found.");

            var bookingLimit = PlanLimits.MaxBookingsPerMonth(tenant.Plan);
            if (bookingLimit.HasValue)
            {
                var utcNow = DateTime.UtcNow;
                var monthCount = await _unitOfWork.BookingRepository.CountBookingsInMonthAsync(
                    tenant.TenantId, utcNow.Year, utcNow.Month, cancellationToken);
                if (monthCount >= bookingLimit.Value)
                    throw new BusinessRuleBrokenException(
                        $"Your {tenant.Plan} plan allows a maximum of {bookingLimit.Value} bookings per month.");
            }

            var tenantId = tenant.TenantId;
            var serviceId = new ServiceId(request.ServiceId);

            var service = await _unitOfWork.ServiceRepository.GetAsync(
                predicate: s => s.ServiceId == serviceId && s.TenantId == tenantId,
                s => s.ServiceStaffs);

            if (service is null || !service.IsActive)
                return BaseResponse<BookingDetailDto>.Failure("Service not found.");

            var minAdvanceHours = service.GetEffectiveMinAdvanceBookingHours(settings.MinAdvanceBookingHours);
            var maxAdvanceDays = service.GetEffectiveMaxAdvanceBookingDays(settings.MaxAdvanceBookingDays);

            // var now = DateTime.UtcNow;
            // var scheduledDateTime = request.ScheduledDate.ToDateTime(request.ScheduledStartTime);
            var now = DateTime.UtcNow;
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tenant.TenantProfile.Timezone);
            var localDateTime = request.ScheduledDate.ToDateTime(request.ScheduledStartTime);
            var scheduledDateTime = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), tz);

            if (scheduledDateTime < now.AddHours(minAdvanceHours))
                return BaseResponse<BookingDetailDto>.Failure("Booking is too soon.");

            if (scheduledDateTime > now.AddDays(maxAdvanceDays))
                return BaseResponse<BookingDetailDto>.Failure("Booking is too far in advance.");

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TenantProfile?.Timezone ?? "UTC");
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            // Resolve which resource to use
            StaffId staffId;
            if (request.StaffId.HasValue)
            {
                staffId = new StaffId(request.StaffId.Value);
                if (!service.IsStaffAssigned(staffId))
                    return BaseResponse<BookingDetailDto>.Failure("Resource not assigned to service.");
            }
            else
            {
                // Auto-assign: find the first active resource that has the requested slot available
                staffId = await ResolveFirstAvailableResourceAsync(
                    service, request.ScheduledDate, request.ScheduledStartTime, cancellationToken, nowLocal, minAdvanceHours);

                if (staffId is null)
                    return BaseResponse<BookingDetailDto>.Failure("Selected time is no longer available.");
            }

            var staff = await _unitOfWork.StaffRepository.FindByIdWithAvailabilityAsync(staffId, cancellationToken);
            if (staff is null || !staff.IsActive)
                return BaseResponse<BookingDetailDto>.Failure("Staff not found.");

            var activeBookings = await _unitOfWork.BookingRepository
                .GetActiveBookingsForStaffOnDateAsync(staffId, request.ScheduledDate, cancellationToken);

            var availableSlots = _slotAvailabilityService.ComputeAvailableSlots(
                service, staff, request.ScheduledDate, activeBookings, nowLocal, minAdvanceHours);

            if (!availableSlots.Contains(request.ScheduledStartTime.ToString("HH:mm")))
                return BaseResponse<BookingDetailDto>.Failure("Selected slot is no longer available.");

            var year = request.ScheduledDate.Year;
            var existingBookings = await _unitOfWork.BookingRepository.GetAllAsync(
                filter: b => b.TenantId == tenantId && b.ScheduledDate.Year == year);
            var bookingReference = $"BK-{year}-{(existingBookings.Count() + 1):D5}";

            var booking = Booking.Create(
                tenantId,
                bookingReference,
                serviceId,
                service.Name,
                staffId,
                staff.FirstName + staff.LastName,
                request.GuestFirstName,
                request.GuestLastName,
                request.GuestEmail,
                request.GuestPhone,
                request.ScheduledDate,
                request.ScheduledStartTime,
                service.DurationMinutes,
                settings.BookingConfirmationMode,
                service.Price,
                service.CurrencyCode,
                request.CustomerNotes);

            var rawToken = _tokenService.GenerateRefreshTokenRaw();
            var tokenHash = _tokenService.HashToken(rawToken);

            var tokenExpiresAt = scheduledDateTime.AddHours(-settings.CancellationCutoffHours);
            var cancellationToken_ = GuestCancellationToken.Create(
                booking.Guest.GuestId,
                tenantId,
                tokenHash,
                tokenExpiresAt);

            _unitOfWork.BookingRepository.Add(booking);
            _unitOfWork.GuestCancellationTokenRepository.Add(cancellationToken_);

            await _unitOfWork.CompleteAsync(cancellationToken);

            var cancellationUrl = _appUrlService.GetGuestCancellationUrl(request.TenantSlug, rawToken);

            await SendConfirmationEmailAsync(booking, tenant.BusinessName, cancellationUrl, settings.CancellationCutoffHours, cancellationToken);

            var dto = booking.ToDetailDto(service.Name, staff.FirstName);
            return BaseResponse<BookingDetailDto>.Success(dto);
        }

        private async Task<StaffId?> ResolveFirstAvailableResourceAsync(
            Service service,
            DateOnly date,
            TimeOnly startTime,
            CancellationToken cancellationToken,
            DateTime nowLocal,
            int minHours)
        {
            var requestedSlot = startTime.ToString("HH:mm");

            foreach (var sr in service.ServiceStaffs)
            {
                var resource = await _unitOfWork.StaffRepository
                    .FindByIdWithAvailabilityAsync(sr.StaffId, cancellationToken);

                if (resource is null || !resource.IsActive)
                    continue;

                var activeBookings = await _unitOfWork.BookingRepository
                    .GetActiveBookingsForStaffOnDateAsync(sr.StaffId, date, cancellationToken);

                var slots = _slotAvailabilityService.ComputeAvailableSlots(service, resource, date, activeBookings, nowLocal, minHours);

                if (slots.Contains(requestedSlot))
                    return sr.StaffId;
            }

            return null;
        }

        private async Task SendConfirmationEmailAsync(
            Booking booking,
            string businessName,
            string cancellationUrl,
            int cancellationCutoffHours,
            CancellationToken ct)
        {
            var guest = booking.Guest;
            var scheduledDisplay = $"{booking.ScheduledDate:dddd, MMMM d, yyyy} at {booking.ScheduledStartTime:h:mm tt}";

            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""></head>
<body style=""font-family: Arial, sans-serif; color: #1a1a1a; max-width: 600px; margin: 0 auto; padding: 24px;"">
  <h2 style=""color: #2d2d2d;"">Your booking is confirmed</h2>
  <p>Hi {guest.FirstName},</p>
  <p>Your appointment with <strong>{businessName}</strong> has been confirmed.</p>

  <table style=""width: 100%; border-collapse: collapse; margin: 24px 0;"">
    <tr>
      <td style=""padding: 8px 0; color: #666; width: 40%;"">Booking reference</td>
      <td style=""padding: 8px 0; font-weight: bold;"">{booking.BookingReference}</td>
    </tr>
    <tr>
      <td style=""padding: 8px 0; color: #666;"">Service</td>
      <td style=""padding: 8px 0;"">{booking.ServiceName}</td>
    </tr>
    <tr>
      <td style=""padding: 8px 0; color: #666;"">Staff</td>
      <td style=""padding: 8px 0;"">{booking.ResourceName}</td>
    </tr>
    <tr>
      <td style=""padding: 8px 0; color: #666;"">Date &amp; time</td>
      <td style=""padding: 8px 0;"">{scheduledDisplay}</td>
    </tr>
    <tr>
      <td style=""padding: 8px 0; color: #666;"">Duration</td>
      <td style=""padding: 8px 0;"">{booking.DurationMinutes} minutes</td>
    </tr>
  </table>

  <p style=""margin-top: 32px; color: #555;"">Need to cancel? Use the button below. This link expires {cancellationCutoffHours} hours before your appointment.</p>

  <a href=""{cancellationUrl}""
     style=""display: inline-block; margin-top: 8px; padding: 12px 24px;
            background-color: #dc2626; color: #ffffff; text-decoration: none;
            border-radius: 6px; font-weight: bold;"">
    Cancel booking
  </a>

  <p style=""margin-top: 32px; font-size: 12px; color: #999;"">
    If the button does not work, copy and paste this link into your browser:<br>
    {cancellationUrl}
  </p>
</body>
</html>";

            await _notificationService.SendEmailAsync(
                guest.Email,
                $"Booking confirmed — {booking.BookingReference}",
                body);
        }
    }
}
