using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CheckoutBooking
{
    public class CheckoutBookingHandler : ICommandHandler<CheckoutBookingCommand, CheckoutBookingResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public CheckoutBookingHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<CheckoutBookingResult> Handle(CheckoutBookingCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Members, t => t.Bookings]);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Isolated tenant context could not be verified.");

            var scannerUserId = _userContext.GetCurrentUserId();
            var scanner = tenant.Members.FirstOrDefault(m => m.UserId == scannerUserId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("Scanner device is not linked to an active staff account.");

            var bookingBeforeCheckout = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == command.BookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");
            var amountSettled = bookingBeforeCheckout.RemainingBalance; // read before CheckOutBooking zeroes it out

            var booking = tenant.CheckOutBooking(command.BookingId, scanner.BranchId);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new CheckoutBookingResult(
                booking.BookingId.Value,
                booking.BookingReference,
                booking.ServiceCompletedAt!.Value,
                amountSettled,
                booking.CurrencyCode);
        }
    }
}
