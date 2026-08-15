using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ReassignBooking
{
    public class ReassignBookingCommandHandler : ICommandHandler<ReassignBookingCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public ReassignBookingCommandHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(ReassignBookingCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to reassign this appointment. No authenticated tenant context was found.");

            // GetForWalkInAvailabilityAsync already hydrates Branches + Members + Services.ServiceProviders
            // + Bookings in one query — exactly what ReassignBooking's validation needs, no new
            // aggregate-load method required.
            var tenant = await _unitOfWork.TenantRepository.GetForWalkInAvailabilityAsync(tenantId, cancellationToken);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Failed to reassign this appointment. Isolated tenant context could not be verified.");

            tenant.ReassignBooking(command.BookingId, command.NewStaffId);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
