using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.ValidateCancellationToken
{
    internal sealed class ValidateCancellationTokenQueryHandler
        : IQueryHandler<ValidateCancellationTokenQuery, BaseResponse<CancellationTokenValidationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;

        public ValidateCancellationTokenQueryHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
        }

        public async Task<BaseResponse<CancellationTokenValidationDto>> Handle(
            ValidateCancellationTokenQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BaseResponse<CancellationTokenValidationDto>.Failure("Token is required.");

            var tokenHash = _tokenService.HashToken(request.Token);
            var token = await _unitOfWork.GuestCancellationTokenRepository
                .FindByTokenHashAsync(tokenHash, cancellationToken);

            if (token is null)
                return BaseResponse<CancellationTokenValidationDto>.Failure("Invalid cancellation link.");

            if (token.IsUsed)
                return BaseResponse<CancellationTokenValidationDto>.Failure("This cancellation link has already been used.");

            if (token.IsExpired)
                return BaseResponse<CancellationTokenValidationDto>.Failure("This cancellation link has expired.");

            var booking = token.Guest?.Booking;
            if (booking is null)
                return BaseResponse<CancellationTokenValidationDto>.Failure("Booking not found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == booking.TenantId,
                t => t.TenantPaymentPolicy);

            var policy = tenant?.TenantPaymentPolicy;

            return BaseResponse<CancellationTokenValidationDto>.Success(
                booking.ToCancellationValidationDto(token.Guest!, policy));
        }
    }
}
