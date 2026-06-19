using ApexBooking.Core.Application.Interfaces;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBookingByToken;

internal sealed class CancelBookingByTokenCommandHandler : ICommandHandler<CancelBookingByTokenCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPushNotificationService _pushService;
    private readonly IRealtimeNotificationService _realtimeService;

    public CancelBookingByTokenCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPushNotificationService pushService,
        IRealtimeNotificationService realtimeService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _pushService = pushService;
        _realtimeService = realtimeService;
    }

    public async Task Handle(CancelBookingByTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashToken(request.Token);
        var token = await _unitOfWork.GuestCancellationTokenRepository
            .FindByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
            throw new UnauthorizedException("Invalid cancellation link.");

        var booking = token.Guest?.Booking;
        if (booking is null)
            throw new NotFoundException("Booking not found.");

        var tenant = await _unitOfWork.TenantRepository.GetAsync(
            predicate: t => t.TenantId == booking.TenantId,
            includes: t => t.TenantSettings);

        if (tenant?.TenantSettings is null)
            throw new NotFoundException("Tenant settings not found.");

        token.MarkAsUsed();
        booking.GuestCancel(tenant.TenantSettings.CancellationCutoffHours, request.Reason ?? "Cancelled by guest.");

        _unitOfWork.GuestCancellationTokenRepository.Update(token);
        _unitOfWork.BookingRepository.Update(booking);

        var admins = await _unitOfWork.UserRepository.GetUsersByRoleAsync(booking.TenantId, UserRole.TenantAdmin);
        var notifications = new List<Notification>();
        foreach (var admin in admins)
        {
            var n = Notification.Create(
                admin.Id,
                NotificationRecipientType.TenantAdmin,
                booking.TenantId,
                NotificationEventType.BookingCancelled,
                "Booking Cancelled",
                $"Booking {booking.BookingReference} was cancelled by the guest.");
            _unitOfWork.NotificationRepository.Add(n);
            notifications.Add(n);
        }

        var staff = await _unitOfWork.StaffRepository.GetByIdAsync(booking.StaffId);
        Notification? staffNotification = null;
        if (staff?.UserId.HasValue == true)
        {
            staffNotification = Notification.Create(
                staff.UserId.Value,
                NotificationRecipientType.Staff,
                booking.TenantId,
                NotificationEventType.BookingCancelled,
                "Booking Cancelled",
                $"Booking {booking.BookingReference} was cancelled by the guest.");
            _unitOfWork.NotificationRepository.Add(staffNotification);
        }

        await _unitOfWork.CompleteAsync(cancellationToken);

        if (notifications.Count > 0)
        {
            await _pushService.SendAsync(
                notifications.Select(n => n.RecipientId).ToList(),
                "Booking Cancelled",
                $"Booking {booking.BookingReference} was cancelled by the guest.",
                NotificationEventType.BookingCancelled.ToString(),
                cancellationToken);
            await Task.WhenAll(notifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));
        }

        if (staffNotification is not null)
        {
            await _pushService.SendAsync(
                [staffNotification.RecipientId],
                "Booking Cancelled",
                $"Booking {booking.BookingReference} was cancelled by the guest.",
                NotificationEventType.BookingCancelled.ToString(),
                cancellationToken);
            await _realtimeService.SendAsync(staffNotification.RecipientId, staffNotification.ToDto());
        }
    }
}