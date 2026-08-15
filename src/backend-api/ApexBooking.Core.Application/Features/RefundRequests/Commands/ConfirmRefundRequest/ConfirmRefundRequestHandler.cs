using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest
{
    public class ConfirmRefundRequestHandler : ICommandHandler<ConfirmRefundRequestCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestStore _refundRequestStore;
        private readonly IFileStorageService _fileStorage;
        private readonly IUserContextService _userContext;

        public ConfirmRefundRequestHandler(
            IUnitOfWork unitOfWork,
            IRefundRequestStore refundRequestStore,
            IFileStorageService fileStorage,
            IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _refundRequestStore = refundRequestStore;
            _fileStorage = fileStorage;
            _userContext = userContext;
        }

        public async Task Handle(ConfirmRefundRequestCommand command, CancellationToken cancellationToken)
        {
            var request = await _refundRequestStore.GetByIdAsync(command.RefundRequestId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Refund request not found.");

            var tenantId = _userContext.GetCurrentTenantId();
            if (request.TenantId != tenantId)
                throw new BusinessRuleBrokenException("Refund request not found.");

            var userId = _userContext.GetCurrentUserId();

            var fileName = $"{tenantId!.Value}/{request.Id}/{Guid.NewGuid()}{command.ReceiptFileExtension}";
            var receiptUrl = await _fileStorage.SaveAsync(command.ReceiptContent, fileName, command.ReceiptContentType, cancellationToken);

            request.Confirm(userId, receiptUrl);
            await _refundRequestStore.UpdateAsync(request, cancellationToken);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == request.TenantId,
                includes: [t => t.Bookings]);

            var booking = tenant?.Bookings.FirstOrDefault(b => b.BookingId.Value == request.BookingId);
            if (booking is null)
                return;

            booking.ConfirmReviewedRefund(request.RequestedAmount, receiptUrl);
            _unitOfWork.TenantRepository.Update(tenant!);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
