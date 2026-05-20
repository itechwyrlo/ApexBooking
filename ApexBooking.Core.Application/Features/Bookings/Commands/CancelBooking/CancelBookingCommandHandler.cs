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

    public CancelBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContextService userContextService)
    {
        _unitOfWork = unitOfWork;
        _userContextService = userContextService;
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
        foreach (var admin in admins)
        {
            _unitOfWork.NotificationRepository.Add(Notification.Create(
                admin.Id,
                NotificationRecipientType.TenantAdmin,
                tenantId,
                NotificationEventType.BookingCancelled,
                "Booking Cancelled",
                $"Booking {booking.BookingReference} was cancelled."));
        }

        await _unitOfWork.CompleteAsync(cancellationToken);
    }
}