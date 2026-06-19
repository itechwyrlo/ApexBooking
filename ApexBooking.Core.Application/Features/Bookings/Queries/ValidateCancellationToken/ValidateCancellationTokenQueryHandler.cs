using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.ValidateCancellationToken
{
    internal sealed class ValidateCancellationTokenQueryHandler
        : IQueryHandler<ValidateCancellationTokenQuery, CancellationTokenValidationDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public ValidateCancellationTokenQueryHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<CancellationTokenValidationDto> Handle(
            ValidateCancellationTokenQuery request,
            CancellationToken cancellationToken)
        {
            var tokenHash = _tokenService.HashToken(request.Token);
            var token = await _unitOfWork.GuestCancellationTokenRepository
                .FindByTokenHashAsync(tokenHash, cancellationToken);

            if (token is null)
                throw new NotFoundException("Invalid cancellation link.");

            token.EnsureNotUsed();
            token.EnsureNotExpired();

            var booking = token.Guest?.Booking;
            if (booking is null)
                throw new NotFoundException("Booking not found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == booking.TenantId,
                t => t.TenantPaymentPolicy);

            return booking.ToCancellationValidationDto(token.Guest!, tenant?.TenantPaymentPolicy);
        }
    }
}
