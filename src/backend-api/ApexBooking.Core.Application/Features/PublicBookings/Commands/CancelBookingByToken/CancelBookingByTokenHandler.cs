using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Commands.CancelBookingByToken
{
    public class CancelBookingByTokenHandler : ICommandHandler<CancelBookingByTokenCommand>
    {
        private const string DefaultReason = "Cancelled by customer via online request";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICancellationTokenService _cancellationTokenService;

        public CancelBookingByTokenHandler(IUnitOfWork unitOfWork, ICancellationTokenService cancellationTokenService)
        {
            _unitOfWork = unitOfWork;
            _cancellationTokenService = cancellationTokenService;
        }

        public async Task Handle(CancelBookingByTokenCommand command, CancellationToken cancellationToken)
        {
            if (!_cancellationTokenService.TryValidate(command.Token, out var payload))
                throw new BusinessRuleBrokenException("This cancellation link is invalid or could not be verified.");

            // Tenant does not implement ITenantEntity, so the global EF query filter doesn't cover
            // this lookup anyway — same anonymous-resolution shape as GetBookingStatusByTicketHandler
            // and ProcessPaymentWebhookCommandHandler, neither of which need to set an ambient
            // tenant either: Booking is already loaded and tracked as part of this same aggregate,
            // not re-queried, so the global filter never gets a chance to apply.
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == payload.TenantId,
                includes: [t => t.Bookings, t => t.BookingPolicy!, t => t.PaymentPolicy!]);

            if (tenant == null)
                throw new BusinessRuleBrokenException("This cancellation link could not be resolved to a business workspace.");

            var reason = string.IsNullOrWhiteSpace(command.Reason) ? DefaultReason : command.Reason;
            tenant.CancelBookingByCustomer(payload.BookingId.Value, reason, command.EwalletProvider, command.EwalletNumber, command.EwalletName);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
