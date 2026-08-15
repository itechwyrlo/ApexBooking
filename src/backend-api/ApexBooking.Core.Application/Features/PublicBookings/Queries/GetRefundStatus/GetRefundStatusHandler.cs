using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetRefundStatus
{
    public class GetRefundStatusHandler : IQueryHandler<GetRefundStatusQuery, RefundStatusDto>
    {
        private readonly ICancellationTokenService _cancellationTokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;

        public GetRefundStatusHandler(
            ICancellationTokenService cancellationTokenService,
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore)
        {
            _cancellationTokenService = cancellationTokenService;
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
        }

        public async Task<RefundStatusDto> Handle(GetRefundStatusQuery query, CancellationToken cancellationToken)
        {
            if (!_cancellationTokenService.TryValidate(query.Token, out var payload))
                throw new BusinessRuleBrokenException("This refund status link is invalid or could not be verified.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == payload.TenantId,
                includes: [t => t.Bookings, t => t.BusinessProfile!]);

            var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == payload.BookingId.Value)
                ?? throw new BusinessRuleBrokenException("This refund status link could not be resolved to a booking.");

            var request = await _refundRequestStore.GetByBookingIdAsync(payload.BookingId.Value, cancellationToken);

            return new RefundStatusDto(
                booking.BookingReference,
                request?.Status,
                request?.RequestedAmount ?? booking.RefundedAmount,
                booking.CurrencyCode,
                tenant?.BusinessProfile?.ContactPhoneNumber,
                request?.ReceiptUrl
            );
        }
    }
}
