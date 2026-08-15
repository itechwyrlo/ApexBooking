using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.MarkBookingNoShow
{
    public class MarkBookingNoShowHandler : ICommandHandler<MarkBookingNoShowCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public MarkBookingNoShowHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(MarkBookingNoShowCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to mark appointment as a no-show. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Bookings);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to mark appointment as a no-show. Isolated tenant context could not be verified.");

            tenant.FlagBookingAsNoShow(command.BookingId);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
