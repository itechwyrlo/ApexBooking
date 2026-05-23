using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using ApexBooking.Core.Domain.Services.Slot;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CreateBooking
{
    internal sealed class CreateBookingCommandHandler : ICommandHandler<CreateBookingCommand, BookingDetailDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SlotAvailabilityService _slotAvailabilityService;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly ITokenService _tokenService;
        private readonly IAppUrlService _appUrlService;

        public CreateBookingCommandHandler(
            IUnitOfWork unitOfWork,
            SlotAvailabilityService slotAvailabilityService,
            IBookingNotificationService bookingNotificationService,
            ITokenService tokenService,
            IAppUrlService appUrlService)
        {
            _unitOfWork = unitOfWork;
            _slotAvailabilityService = slotAvailabilityService;
            _bookingNotificationService = bookingNotificationService;
            _tokenService = tokenService;
            _appUrlService = appUrlService;
        }

        public async Task<BookingDetailDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(request.TenantSlug);

            if (tenant is null)
                throw new NotFoundException("Tenant not found.");

            var settings = tenant.TenantSettings;
            if (settings is null)
                throw new NotFoundException("Tenant settings not found.");

            var utcNow = DateTime.UtcNow;
            var monthCount = await _unitOfWork.BookingRepository.CountBookingsInMonthAsync(
                tenant.TenantId, utcNow.Year, utcNow.Month, cancellationToken);

            tenant.EnforceMonthlyBookingLimit(monthCount);

            var tenantId = tenant.TenantId;
            var serviceId = new ServiceId(request.ServiceId);

            var service = await _unitOfWork.ServiceRepository.GetAsync(
                predicate: s => s.ServiceId == serviceId && s.TenantId == tenantId,
                s => s.ServiceStaffs);

            if (service is null || !service.IsActive)
                throw new NotFoundException("Service not found.");

            var minAdvanceHours = service.GetEffectiveMinAdvanceBookingHours(settings.MinAdvanceBookingHours);

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TenantProfile?.Timezone ?? "UTC");
            var now = DateTime.UtcNow;
            var localDateTime = request.ScheduledDate.ToDateTime(request.ScheduledStartTime);
            var scheduledDateTime = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), timeZone);
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(now, timeZone);

            service.ValidateBookingWindow(scheduledDateTime, now, settings.MinAdvanceBookingHours, settings.MaxAdvanceBookingDays);

            StaffId staffId;
            if (request.StaffId.HasValue)
            {
                staffId = new StaffId(request.StaffId.Value);
                service.EnsureStaffAssigned(staffId);
            }
            else
            {
                var candidates = new List<(Staff staff, IReadOnlyList<Booking> activeBookings)>();
                foreach (var sr in service.ServiceStaffs)
                {
                    var candidate = await _unitOfWork.StaffRepository.FindByIdWithAvailabilityAsync(sr.StaffId, cancellationToken);
                    if (candidate is null || !candidate.IsActive)
                        continue;

                    var bookings = await _unitOfWork.BookingRepository
                        .GetActiveBookingsForStaffOnDateAsync(sr.StaffId, request.ScheduledDate, cancellationToken);

                    candidates.Add((candidate, bookings));
                }

                staffId = _slotAvailabilityService.FindFirstAvailableStaff(
                    service, candidates, request.ScheduledDate, request.ScheduledStartTime, nowLocal, minAdvanceHours);
            }

            var staff = await _unitOfWork.StaffRepository.FindByIdWithAvailabilityAsync(staffId, cancellationToken);
            if (staff is null)
                throw new NotFoundException("Staff not found.");

            staff.EnsureAvailableForBooking();

            var activeBookings = await _unitOfWork.BookingRepository
                .GetActiveBookingsForStaffOnDateAsync(staffId, request.ScheduledDate, cancellationToken);

            _slotAvailabilityService.ValidateSlotAvailable(
                service, staff, request.ScheduledDate, activeBookings, nowLocal, minAdvanceHours, request.ScheduledStartTime);

            var year = DateTime.UtcNow.Year;
            var nextSequence = await _unitOfWork.BookingRepository.GetNextBookingSequenceAsync(tenantId, year, cancellationToken);
            var bookingReference = $"BK-{year}-{nextSequence:D5}";

            var requiresPaymentAtBooking = tenant.TenantPaymentPolicy?.PaymentRequired ?? false;

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
                requiresPaymentAtBooking,
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

            var admins = await _unitOfWork.UserRepository.GetUsersByRoleAsync(tenantId, UserRole.TenantAdmin);
            foreach (var admin in admins)
            {
                _unitOfWork.NotificationRepository.Add(Notification.Create(
                    admin.Id,
                    NotificationRecipientType.TenantAdmin,
                    tenantId,
                    NotificationEventType.BookingCreated,
                    "New Booking",
                    $"New booking by {booking.Guest.FirstName} {booking.Guest.LastName} for {booking.ServiceName} with {booking.ResourceName} on {booking.ScheduledDate:MMM d}."));
            }

            await _unitOfWork.CompleteAsync(cancellationToken);

            var cancellationUrl = _appUrlService.GetGuestCancellationUrl(request.TenantSlug, rawToken);

            if (settings.BookingConfirmationMode == BookingConfirmationMode.Manual)
            {
                await _bookingNotificationService.SendPendingApprovalEmailAsync(booking, tenant.BusinessName, cancellationToken);
            }
            else
            {
                await _bookingNotificationService.SendConfirmationEmailAsync(booking, tenant.BusinessName, cancellationUrl, settings.CancellationCutoffHours, cancellationToken);
            }

            return booking.ToDetailDto(service.Name, staff.FirstName);
        }
    }
}
