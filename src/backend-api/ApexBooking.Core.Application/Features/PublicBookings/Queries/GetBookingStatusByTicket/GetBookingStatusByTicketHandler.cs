using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetBookingStatusByTicket
{
    public class GetBookingStatusByTicketHandler : IQueryHandler<GetBookingStatusByTicketQuery, PublicBookingStatusDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITicketTokenService _ticketTokenService;

        public GetBookingStatusByTicketHandler(IUnitOfWork unitOfWork, ITicketTokenService ticketTokenService)
        {
            _unitOfWork = unitOfWork;
            _ticketTokenService = ticketTokenService;
        }

        public async Task<PublicBookingStatusDto> Handle(GetBookingStatusByTicketQuery query, CancellationToken cancellationToken)
        {
            // The signed ticket token is itself the trusted source of tenant identity here — this
            // deliberately bypasses ITenantEntity/subdomain resolution so the poll works regardless
            // of which host the browser lands on after the PayMongo redirect.
            if (!_ticketTokenService.TryValidate(query.TicketToken, out var payload))
                throw new BusinessRuleBrokenException("This booking ticket is invalid or could not be verified.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == payload.TenantId,
                includes: [t => t.Bookings, t => t.Members, t => t.Services, t => t.Branches]);

            if (tenant == null)
                throw new BusinessRuleBrokenException("This booking ticket could not be resolved to a business workspace.");

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId == payload.BookingId)
                ?? throw new BusinessRuleBrokenException("The booking referenced by this ticket no longer exists.");

            var service = tenant.Services.FirstOrDefault(s => s.ServiceId == booking.ServiceId);
            var staff = tenant.Members.FirstOrDefault(m => m.TenantMemberId == booking.StaffId);
            var branch = tenant.Branches.FirstOrDefault(b => b.BranchId == booking.BranchId);

            return new PublicBookingStatusDto(
                booking.BookingId.Value,
                booking.BookingReference,
                booking.Status,
                service?.Name ?? string.Empty,
                staff is not null ? $"{staff.FirstName} {staff.LastName}".Trim() : string.Empty,
                branch?.BranchName ?? string.Empty,
                booking.ScheduledDate,
                booking.ScheduledStartTime,
                booking.RequiresUpfrontPayment,
                booking.AmountDue,
                booking.CurrencyCode);
        }
    }
}
