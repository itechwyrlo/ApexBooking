using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetCheckoutDetails
{
    public class GetCheckoutDetailsHandler : IQueryHandler<GetCheckoutDetailsQuery, CheckoutDetailsDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public GetCheckoutDetailsHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<CheckoutDetailsDto> Handle(GetCheckoutDetailsQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members);

            var scannerUserId = _userContext.GetCurrentUserId();
            var scanner = tenant?.Members.FirstOrDefault(m => m.UserId == scannerUserId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("Scanner device is not linked to an active staff account.");

            var row = await _unitOfWork.TenantRepository.GetBookingCheckoutDetailAsync(tenantId, query.BookingId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            if (row.BranchId != scanner.BranchId.Value)
                throw new BusinessRuleBrokenException("This boarding pass belongs to a different branch and cannot be scanned here.");

            if (row.CheckedInAt is null)
                throw new BusinessRuleBrokenException("This booking hasn't been checked in yet.");

            if (row.Status == BookingStatus.Completed)
                throw new BusinessRuleBrokenException("This booking has already been completed.");

            if (row.Status == BookingStatus.Cancelled)
                throw new BusinessRuleBrokenException("This booking was cancelled.");

            if (row.Status == BookingStatus.NoShow)
                throw new BusinessRuleBrokenException("This booking was marked as a no-show.");

            var amountPaidOnline = row.PaymentConfirmedVia == PaymentConfirmationMethod.Online ? row.AmountDue : 0m;
            var remainingBalance = row.ServicePriceAtBooking.HasValue
                ? Math.Max(0m, row.ServicePriceAtBooking.Value - amountPaidOnline - row.InVisitAmountCollected)
                : (row.PaymentConfirmedVia is null ? row.AmountDue : 0m);

            return new CheckoutDetailsDto(
                row.BookingId,
                row.BookingReference,
                row.CustomerName,
                row.CustomerEmail,
                row.CustomerPhone,
                row.ServiceName,
                row.StaffName,
                row.ScheduledDate,
                row.ScheduledStartTime,
                amountPaidOnline,
                remainingBalance,
                row.CurrencyCode);
        }
    }
}
