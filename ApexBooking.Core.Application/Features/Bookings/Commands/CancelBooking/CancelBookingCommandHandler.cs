using ApexBooking.Core.Application.Interfaces;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking;

internal sealed class CancelBookingCommandHandler : ICommandHandler<CancelBookingCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _userContextService;
    private readonly IPushNotificationService _pushService;
    private readonly IRealtimeNotificationService _realtimeService;

    public CancelBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContextService userContextService,
        IPushNotificationService pushService,
        IRealtimeNotificationService realtimeService)
    {
        _unitOfWork = unitOfWork;
        _userContextService = userContextService;
        _pushService = pushService;
        _realtimeService = realtimeService;
    }

    public async Task Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var bookingId = new BookingId(request.BookingId);
        var userId = _userContextService.GetCurrentUserId();
        var tenantId = _userContextService.GetCurrentTenantId();

        var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
        if (booking is null)
            throw new NotFoundException("Booking not found.");

        var tenant = await _unitOfWork.TenantRepository.GetAsync(
            predicate: t => t.TenantId == tenantId,
            includes: t => t.TenantSettings);

        if (tenant?.TenantSettings is null)
            throw new NotFoundException("Tenant settings not found.");

        booking.Cancel(userId, tenant.TenantSettings.CancellationCutoffHours, request.Reason);

        _unitOfWork.BookingRepository.Update(booking);

        var admins = await _unitOfWork.UserRepository.GetUsersByRoleAsync(tenantId, UserRole.TenantAdmin);
        var adminNotifications = new List<Notification>();
        foreach (var admin in admins)
        {
            var n = Notification.Create(
                admin.Id,
                NotificationRecipientType.TenantAdmin,
                tenantId,
                NotificationEventType.BookingCancelled,
                "Booking Cancelled",
                $"Booking {booking.BookingReference} was cancelled.");
            _unitOfWork.NotificationRepository.Add(n);
            adminNotifications.Add(n);
        }

        await _unitOfWork.CompleteAsync(cancellationToken);

        await _pushService.SendAsync(
            adminNotifications.Select(n => n.RecipientId).ToList(),
            "Booking Cancelled",
            $"Booking {booking.BookingReference} was cancelled.",
            NotificationEventType.BookingCancelled.ToString(),
            cancellationToken);
        await Task.WhenAll(adminNotifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));
    }
}