using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.AdmitBooking
{
    public class AdmitBookingHandler : ICommandHandler<AdmitBookingCommand, ScanArrivalResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public AdmitBookingHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<ScanArrivalResult> Handle(AdmitBookingCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to admit appointment. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Members, t => t.Bookings]);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to admit appointment. Isolated tenant context could not be verified.");

            // The admitting staff's active branch is their own deployment location — never client-supplied.
            var admittingUserId = _userContext.GetCurrentUserId();
            var admittingStaff = tenant.Members.FirstOrDefault(m => m.UserId == admittingUserId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("Your account is not linked to an active staff account.");

            var (booking, wasFirstAdmission) = tenant.RecordBookingArrival(new(command.BookingId), admittingStaff.BranchId);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new ScanArrivalResult(
                booking.BookingId.Value,
                booking.BookingReference,
                booking.Status.ToString(),
                booking.CheckedInAt!.Value,
                wasFirstAdmission);
        }
    }
}
