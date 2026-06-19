using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Clients.Queries.GetClients
{
    internal sealed class GetClientsQueryHandler
        : IQueryHandler<GetClientsQuery, PagedResult<ClientSummaryDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetClientsQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<PagedResult<ClientSummaryDto>> Handle(
            GetClientsQuery query,
            CancellationToken cancellationToken)
        {
            IEnumerable<Domain.Entities.Booking> bookings;

            if (_contextService.GetUserRole() == "staff")
            {
                var userId = _contextService.GetCurrentUserId();
                var staff = await _unitOfWork.StaffRepository
                    .FindByUserIdAsync(userId, cancellationToken)
                    .ConfigureAwait(false);

                if (staff is null)
                    throw new UnauthorizedAccessException("Staff profile not found.");

                bookings = await _unitOfWork.BookingRepository
                    .GetAllAsync(b => b.StaffId == staff.StaffId, b => b.Guest)
                    .ConfigureAwait(false);
            }
            else
            {
                bookings = await _unitOfWork.BookingRepository
                    .GetAllAsync(filter: null, b => b.Guest)
                    .ConfigureAwait(false);
            }

            var clients = bookings
                .GroupBy(b => b.Guest.Email)
                .Select(g =>
                {
                    var mostRecent = g.OrderByDescending(b => b.CreatedAt).First();
                    var lastVisit = g
                        .Where(b => b.Status == BookingStatus.Completed)
                        .OrderByDescending(b => b.ScheduledDate)
                        .Select(b => (DateOnly?)b.ScheduledDate)
                        .FirstOrDefault();
                    var totalSpent = g
                        .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.PendingPayment)
                        .Sum(b => b.PriceSnapshot);

                    return new ClientSummaryDto(
                        g.Key,
                        mostRecent.Guest.FullName,
                        mostRecent.Guest.Phone,
                        g.Count(),
                        lastVisit,
                        totalSpent,
                        mostRecent.CurrencyCode);
                })
                .OrderByDescending(c => c.TotalBookings)
                .ToList();

            var pageNumber = query.Param.PageNumber;
            var pageSize = query.Param.PageSize;
            var paged = clients.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult<ClientSummaryDto>(paged, clients.Count);
        }
    }
}
