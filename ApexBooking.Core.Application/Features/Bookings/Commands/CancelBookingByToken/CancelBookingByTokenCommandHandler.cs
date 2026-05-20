using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBookingByToken;

internal sealed class CancelBookingByTokenCommandHandler : ICommandHandler<CancelBookingByTokenCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public CancelBookingByTokenCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
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
        await _unitOfWork.CompleteAsync(cancellationToken);
    }
}