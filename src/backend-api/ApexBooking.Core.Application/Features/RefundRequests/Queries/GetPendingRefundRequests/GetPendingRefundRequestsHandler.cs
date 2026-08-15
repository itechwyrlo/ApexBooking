using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests
{
    public class GetPendingRefundRequestsHandler
        : IQueryHandler<GetPendingRefundRequestsQuery, QueryResult<RefundRequestSummaryDto>>
    {
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;

        public GetPendingRefundRequestsHandler(
            IRefundRequestStore refundRequestStore,
            IUnitOfWork unitOfWork,
            IUserContextService userContext)
        {
            _refundRequestStore = refundRequestStore;
            _unitOfWork = unitOfWork;
            _userContext = userContext;
        }

        public async Task<QueryResult<RefundRequestSummaryDto>> Handle(
            GetPendingRefundRequestsQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _userContext.GetCurrentTenantId();
            var (requests, total) = await _refundRequestStore.GetPendingForTenantAsync(
                tenantId, query.PageNumber, query.PageSize, cancellationToken);

            if (requests.Count == 0)
                return new QueryResult<RefundRequestSummaryDto>(Array.Empty<RefundRequestSummaryDto>(), total);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Bookings, t => t.PaymentPolicy!]);

            var deadlineDays = tenant?.PaymentPolicy?.RefundReviewDeadlineDays ?? 7;

            var result = new List<RefundRequestSummaryDto>();
            foreach (var request in requests)
            {
                var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);
                var customer = booking is null
                    ? null
                    : await _unitOfWork.CustomerRepository.GetAsync(predicate: c => c.CustomerId == booking.CustomerId);

                result.Add(new RefundRequestSummaryDto(
                    request.Id,
                    request.BookingId,
                    booking?.BookingReference ?? "(unknown)",
                    customer?.Contact.Name ?? "(unknown)",
                    request.RequestedAmount,
                    booking?.AmountDue ?? request.RequestedAmount,
                    booking?.PayMongoPaymentId,
                    request.CurrencyCode,
                    request.Status,
                    request.RejectionReason,
                    request.CustomerEwalletProvider,
                    request.CustomerEwalletNumber,
                    request.CustomerEwalletName,
                    request.ReceiptUrl,
                    request.CreatedAt,
                    request.CreatedAt.AddDays(deadlineDays)));
            }

            return new QueryResult<RefundRequestSummaryDto>(result, total);
        }
    }
}
