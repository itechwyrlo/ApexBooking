using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBookingByToken
{
    internal sealed class CancelBookingByTokenCommandHandler
        : ICommandHandler<CancelBookingByTokenCommand, BaseResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public CancelBookingByTokenCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<BaseResponse<bool>> Handle(
            CancelBookingByTokenCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BaseResponse<bool>.Failure("Token is required.");

            var tokenHash = _tokenService.HashToken(request.Token);
            var token = await _unitOfWork.GuestCancellationTokenRepository
                .FindByTokenHashAsync(tokenHash, cancellationToken);

            if (token is null)
                return BaseResponse<bool>.Failure("Invalid cancellation link.");

            if (token.IsUsed)
                return BaseResponse<bool>.Failure("This cancellation link has already been used.");

            if (token.IsExpired)
                return BaseResponse<bool>.Failure("This cancellation link has expired.");

            var booking = token.Guest?.Booking;
            if (booking is null)
                return BaseResponse<bool>.Failure("Booking not found.");

            // Enforce cancellation cutoff policy
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == booking.TenantId,
                t => t.TenantSettings);

            if (tenant?.TenantSettings is not null)
            {
                var scheduledDateTime = booking.ScheduledDate.ToDateTime(booking.ScheduledStartTime);
                var hoursUntilBooking = (scheduledDateTime - DateTime.UtcNow).TotalHours;

                if (hoursUntilBooking < tenant.TenantSettings.CancellationCutoffHours)
                    return BaseResponse<bool>.Failure(
                        $"Cancellation is not allowed within {tenant.TenantSettings.CancellationCutoffHours} hours of the scheduled time.",
                        "CANCELLATION_CUTOFF_EXCEEDED");
            }

            try
            {
                token.MarkAsUsed();
                booking.GuestCancel(request.Reason ?? "Cancelled by guest.");
            }
            catch (BusinessRuleBrokenException ex)
            {
                return BaseResponse<bool>.Failure(ex.Message);
            }

            _unitOfWork.GuestCancellationTokenRepository.Update(token);
            _unitOfWork.BookingRepository.Update(booking);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
    }
}
