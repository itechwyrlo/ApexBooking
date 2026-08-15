using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest
{
    public class RejectRefundRequestHandler : ICommandHandler<RejectRefundRequestCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IUserContextService _userContext;

        public RejectRefundRequestHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _userContext = userContext;
        }

        public async Task Handle(RejectRefundRequestCommand command, CancellationToken cancellationToken)
        {
            var request = await _refundRequestStore.GetByIdAsync(command.RefundRequestId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Refund request not found.");

            var tenantId = _userContext.GetCurrentTenantId();
            if (request.TenantId != tenantId)
                throw new BusinessRuleBrokenException("Refund request not found.");

            var userId = _userContext.GetCurrentUserId();
            request.Reject(userId, command.Reason);
            await _refundRequestStore.UpdateAsync(request, cancellationToken);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == request.TenantId,
                includes: [t => t.Bookings]);

            var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);
            if (booking is null)
                return;

            booking.RejectReviewedRefund(command.Reason);
            _unitOfWork.TenantRepository.Update(tenant!);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
