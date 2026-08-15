using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetCancellableBooking
{
    public class GetCancellableBookingHandler : IQueryHandler<GetCancellableBookingQuery, CancellableBookingDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICancellationTokenService _cancellationTokenService;

        public GetCancellableBookingHandler(IUnitOfWork unitOfWork, ICancellationTokenService cancellationTokenService)
        {
            _unitOfWork = unitOfWork;
            _cancellationTokenService = cancellationTokenService;
        }

        public async Task<CancellableBookingDto> Handle(GetCancellableBookingQuery query, CancellationToken cancellationToken)
        {
            if (!_cancellationTokenService.TryValidate(query.Token, out var payload))
                throw new BusinessRuleBrokenException("This cancellation link is invalid or could not be verified.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == payload.TenantId,
                includes: [t => t.Bookings, t => t.Services, t => t.Members, t => t.Branches, t => t.BookingPolicy!, t => t.PaymentPolicy!]);

            if (tenant == null)
                throw new BusinessRuleBrokenException("This cancellation link could not be resolved to a business workspace.");

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId == payload.BookingId)
                ?? throw new BusinessRuleBrokenException("The booking referenced by this link no longer exists.");

            var service = tenant.Services.FirstOrDefault(s => s.ServiceId == booking.ServiceId);
            var staff = tenant.Members.FirstOrDefault(m => m.TenantMemberId == booking.StaffId);
            var branch = tenant.Branches.FirstOrDefault(b => b.BranchId == booking.BranchId);

            var (canCancelOnline, unavailableReason) = Evaluate(booking.Status, booking.ScheduledDate, booking.ScheduledStartTime, tenant.BookingPolicy?.CancellationCutoffHours ?? 0);

            var isRefundEligible = tenant.PaymentPolicy?.RefundEnabled == true
                && booking.RequiresUpfrontPayment
                && booking.PaymentConfirmedVia == PaymentConfirmationMethod.Online;

            return new CancellableBookingDto(
                booking.BookingReference,
                service?.Name ?? string.Empty,
                staff is not null ? $"{staff.FirstName} {staff.LastName}".Trim() : string.Empty,
                branch?.BranchName ?? string.Empty,
                booking.ScheduledDate,
                booking.ScheduledStartTime,
                canCancelOnline,
                unavailableReason,
                isRefundEligible);
        }

        // Mirrors Tenant.CancelBookingByCustomer's own guards exactly, so the preview never shows
        // "you can cancel" for a booking that would then be rejected when actually attempted.
        private static (bool CanCancelOnline, string? UnavailableReason) Evaluate(
            BookingStatus status, DateOnly scheduledDate, TimeOnly scheduledStartTime, int cutoffHours)
        {
            if (status != BookingStatus.Scheduled)
            {
                var reason = status switch
                {
                    BookingStatus.Cancelled => "already-cancelled",
                    BookingStatus.Completed => "already-completed",
                    BookingStatus.NoShow => "already-no-show",
                    BookingStatus.PendingPayment => "pending-payment",
                    _ => "unavailable",
                };
                return (false, reason);
            }

            var scheduledAt = scheduledDate.ToDateTime(scheduledStartTime);
            if (DateTime.UtcNow.AddHours(cutoffHours) > scheduledAt)
                return (false, "past-cutoff");

            return (true, null);
        }
    }
}
