using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.ScanArrival
{
    public class ScanArrivalHandler : ICommandHandler<ScanArrivalCommand, ScanArrivalResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITicketTokenService _ticketTokenService;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public ScanArrivalHandler(IUnitOfWork unitOfWork, ITicketTokenService ticketTokenService, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _ticketTokenService = ticketTokenService;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<ScanArrivalResult> Handle(ScanArrivalCommand command, CancellationToken cancellationToken)
        {
            if (!_ticketTokenService.TryValidate(command.Token, out var payload))
                throw new BusinessRuleBrokenException("This boarding pass is invalid or has been tampered with.");

            var scannerUserId = _userContext.GetCurrentUserId();

            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("This boarding pass does not belong to your organization.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Members, t => t.Bookings]);

            if (tenant == null || tenant.TenantId != payload.TenantId)
                throw new BusinessRuleBrokenException("This boarding pass does not belong to your organization.");

            // The scanner's active branch is its own deployment location — the mobile app never supplies it.
            var scanner = tenant.Members.FirstOrDefault(m => m.UserId == scannerUserId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("Scanner device is not linked to an active staff account.");

            var (booking, wasFirstAdmission) = tenant.RecordBookingArrival(payload.BookingId, scanner.BranchId);

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
