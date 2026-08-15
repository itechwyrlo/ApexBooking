using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantBookings
{
    public class GetTenantBookingsHandler : IQueryHandler<GetTenantBookingsQuery, QueryResult<TenantBookingSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public GetTenantBookingsHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<QueryResult<TenantBookingSummary>> Handle(GetTenantBookingsQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load appointments. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Members]);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Failed to load appointments. Workspace environment could not be verified.");

            var currentUserId = _userContext.GetCurrentUserId();
            var currentMember = tenant.Members.FirstOrDefault(m => m.UserId == currentUserId);

            // Staff can only ever see their own bookings — the caller's membership overrides
            // whatever (if anything) the client passed as staffId.
            var staffId = currentMember is { Role: SystemRole.Staff }
                ? currentMember.TenantMemberId.Value
                : query.StaffId;

            var pagedResult = await _unitOfWork.TenantRepository.GetBookingsPageAsync(
                tenantId,
                query.param,
                query.BranchId is { } branchId ? new BranchId(branchId) : null,
                staffId is { } resolvedStaffId ? new TenantMemberId(resolvedStaffId) : null,
                query.Status,
                query.FromDate,
                query.ToDate,
                cancellationToken);

            var mappedItems = pagedResult.data.Select(row =>
            {
                var amountPaidOnline = row.PaymentConfirmedVia == PaymentConfirmationMethod.Online ? row.AmountDue : 0m;
                var remainingBalance = row.ServicePriceAtBooking.HasValue
                    ? Math.Max(0m, row.ServicePriceAtBooking.Value - amountPaidOnline - row.InVisitAmountCollected)
                    : (row.PaymentConfirmedVia is null ? row.AmountDue : 0m);

                return new TenantBookingSummary(
                    row.BookingId,
                    row.BookingReference,
                    row.CustomerName,
                    row.CustomerPhone,
                    row.ServiceName,
                    row.StaffName,
                    row.BranchName,
                    row.ScheduledDate,
                    row.ScheduledStartTime,
                    row.DurationMinutes,
                    row.Status,
                    row.RequiresUpfrontPayment,
                    row.AmountDue,
                    row.CurrencyCode,
                    row.PaymentConfirmedVia,
                    row.CheckedInAt,
                    row.ServiceCompletedAt,
                    row.CancelledAt,
                    row.CancellationReason,
                    row.NoShowAt,
                    row.CustomerId,
                    row.StaffId,
                    row.CreatedAt,
                    amountPaidOnline,
                    remainingBalance);
            });

            return new QueryResult<TenantBookingSummary>(mappedItems, pagedResult.total);
        }
    }
}
