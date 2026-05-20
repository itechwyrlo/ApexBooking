using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ConfirmBooking;

internal sealed class ConfirmBookingCommandHandler : ICommandHandler<ConfirmBookingCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _userContextService;
    private readonly IBookingNotificationService _bookingNotificationService;
    private readonly ITokenService _tokenService;
    private readonly IAppUrlService _appUrlService;

    public ConfirmBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IUserContextService userContextService,
        IBookingNotificationService bookingNotificationService,
        ITokenService tokenService,
        IAppUrlService appUrlService)
    {
        _unitOfWork = unitOfWork;
        _userContextService = userContextService;
        _bookingNotificationService = bookingNotificationService;
        _tokenService = tokenService;
        _appUrlService = appUrlService;
    }

    public async Task Handle(ConfirmBookingCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _userContextService.GetCurrentTenantId();
        var userId = _userContextService.GetCurrentUserId();
        var bookingId = new BookingId(command.BookingId);

        var booking = await _unitOfWork.BookingRepository.GetAsync(
            predicate: b => b.BookingId == bookingId && b.TenantId == tenantId,
            b => b.Guest);

        if (booking is null)
            throw new NotFoundException("Booking not found.");

        var tenant = await _unitOfWork.TenantRepository.GetAsync(
            predicate: t => t.TenantId == tenantId,
            includes: t => t.TenantSettings);

        if (tenant?.TenantSettings is null)
            throw new NotFoundException("Tenant settings not found.");

        booking.Confirm(userId);

        var rawToken = _tokenService.GenerateRefreshTokenRaw();
        var tokenHash = _tokenService.HashToken(rawToken);
        var scheduledDateTime = booking.ScheduledDate.ToDateTime(booking.ScheduledStartTime);
        var tokenExpiresAt = scheduledDateTime.AddHours(-tenant.TenantSettings.CancellationCutoffHours);

        var existingToken = await _unitOfWork.GuestCancellationTokenRepository.GetAsync(
            t => t.GuestId == booking.Guest.GuestId);
        if (existingToken is not null)
            _unitOfWork.GuestCancellationTokenRepository.Remove(existingToken);

        var cancellationToken_ = GuestCancellationToken.Create(
            booking.Guest.GuestId,
            tenantId,
            tokenHash,
            tokenExpiresAt);

        _unitOfWork.BookingRepository.Update(booking);
        _unitOfWork.GuestCancellationTokenRepository.Add(cancellationToken_);

        await _unitOfWork.CompleteAsync(cancellationToken);

        var cancellationUrl = _appUrlService.GetGuestCancellationUrl(tenant.Slug, rawToken);

        await _bookingNotificationService.SendConfirmationEmailAsync(
            booking,
            tenant.BusinessName,
            cancellationUrl,
            tenant.TenantSettings.CancellationCutoffHours,
            cancellationToken);
    }
}
