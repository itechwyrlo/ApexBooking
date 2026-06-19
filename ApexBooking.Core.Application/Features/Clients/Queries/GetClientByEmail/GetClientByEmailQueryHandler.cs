using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Clients.Queries.GetClientByEmail
{
    internal sealed class GetClientByEmailQueryHandler
        : IQueryHandler<GetClientByEmailQuery, ClientDetailDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetClientByEmailQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<ClientDetailDto> Handle(
            GetClientByEmailQuery query,
            CancellationToken cancellationToken)
        {
            var email = query.Email.Trim().ToLowerInvariant();

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
                    .GetAllAsync(
                        b => b.Guest.Email == email && b.StaffId == staff.StaffId,
                        b => b.Guest)
                    .ConfigureAwait(false);
            }
            else
            {
                bookings = await _unitOfWork.BookingRepository
                    .GetAllAsync(b => b.Guest.Email == email, b => b.Guest)
                    .ConfigureAwait(false);
            }

            var bookingList = bookings
                .OrderByDescending(b => b.ScheduledDate)
                .ThenByDescending(b => b.ScheduledStartTime)
                .ToList();

            if (bookingList.Count == 0)
                throw new NotFoundException($"No bookings found for client '{query.Email}'.");

            var mostRecent = bookingList.First();

            var lastVisit = bookingList
                .Where(b => b.Status == BookingStatus.Completed)
                .Select(b => (DateOnly?)b.ScheduledDate)
                .FirstOrDefault();

            var totalSpent = bookingList
                .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.PendingPayment)
                .Sum(b => b.PriceSnapshot);

            return new ClientDetailDto(
                mostRecent.Guest.Email,
                mostRecent.Guest.FullName,
                mostRecent.Guest.Phone,
                bookingList.Count,
                lastVisit,
                totalSpent,
                mostRecent.CurrencyCode,
                bookingList.Select(b => b.ToClientBookingDto()).ToList());
        }
    }
}
