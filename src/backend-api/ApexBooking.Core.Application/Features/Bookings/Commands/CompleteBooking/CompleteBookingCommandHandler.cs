using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CompleteBooking
{
    public class CompleteBookingCommandHandler : ICommandHandler<CompleteBookingCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public CompleteBookingCommandHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(CompleteBookingCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to complete appointment. No authenticated tenant context was found.");

            // Single Database Trip: locate the parent tenant aggregate, explicitly scoped to the caller's own tenant.
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Bookings);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            // Validates the state change and raises BookingCompletedDomainEvent.
            tenant.CompleteBooking(command.BookingId);

            _unitOfWork.TenantRepository.Update(tenant);

            // 🌟 Post-commit dispatch fires SendThankYouEmailOnBookingCompletedHandler automatically.
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
