using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetRefundLog
{
    public class GetRefundLogHandler : IQueryHandler<GetRefundLogQuery, IReadOnlyCollection<RefundLogEntryDto>>
    {
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;

        public GetRefundLogHandler(
            IRefundRequestStore refundRequestStore,
            IUnitOfWork unitOfWork,
            IUserContextService userContext)
        {
            _refundRequestStore = refundRequestStore;
            _unitOfWork = unitOfWork;
            _userContext = userContext;
        }

        public async Task<IReadOnlyCollection<RefundLogEntryDto>> Handle(GetRefundLogQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _userContext.GetCurrentTenantId();
            var requests = await _refundRequestStore.GetProcessedForTenantAsync(tenantId, query.Limit, cancellationToken);

            if (requests.Count == 0)
                return Array.Empty<RefundLogEntryDto>();

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Bookings);

            var result = new List<RefundLogEntryDto>();
            foreach (var request in requests)
            {
                var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);

                result.Add(new RefundLogEntryDto(
                    request.Id,
                    booking?.BookingReference ?? "(unknown)",
                    request.RequestedAmount,
                    request.CurrencyCode,
                    request.Status,
                    request.UpdatedAt));
            }

            return result;
        }
    }
}
