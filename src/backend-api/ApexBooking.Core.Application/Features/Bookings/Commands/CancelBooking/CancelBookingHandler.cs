using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking
{
    public class CancelBookingHandler : ICommandHandler<CancelBookingCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;
        private readonly IUserContextService _userContext;

        public CancelBookingHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity, IUserContextService userContext)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
            _userContext = userContext;
        }

        public async Task Handle(CancelBookingCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to cancel appointment. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Bookings, t => t.BookingPolicy!, t => t.PaymentPolicy!]);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to cancel appointment. Isolated tenant context could not be verified.");

            var currentUserId = _userContext.GetCurrentUserId();
            tenant.CancelBooking(command.BookingId, currentUserId, command.Reason, command.EwalletProvider, command.EwalletNumber, command.EwalletName);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
