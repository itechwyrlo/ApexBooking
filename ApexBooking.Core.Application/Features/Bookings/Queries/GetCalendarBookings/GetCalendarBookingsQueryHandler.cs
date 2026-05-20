using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetCalendarBookings;

internal sealed class GetCalendarBookingsQueryHandler
    : IQueryHandler<GetCalendarBookingsQuery, IReadOnlyList<TenantBookingsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _contextService;

    public GetCalendarBookingsQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
    }

    public async Task<IReadOnlyList<TenantBookingsDto>> Handle(
        GetCalendarBookingsQuery query,
        CancellationToken cancellationToken)
    {
        var tenantId = _contextService.GetCurrentTenantId();

        if (_contextService.GetUserRole() == "staff")
        {
            var userId = _contextService.GetCurrentUserId();
            var staff = await _unitOfWork.StaffRepository
                .FindByUserIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            if (staff is null)
                throw new UnauthorizedAccessException("Staff profile not found.");

            var staffBookings = await _unitOfWork.BookingRepository
                .GetBookingsForMonthAsync(tenantId, query.Year, query.Month, staff.StaffId, cancellationToken)
                .ConfigureAwait(false);

            return staffBookings.Select(b => b.ToTenantDto()).ToList();
        }

        var bookings = await _unitOfWork.BookingRepository
            .GetBookingsForMonthAsync(tenantId, query.Year, query.Month, null, cancellationToken)
            .ConfigureAwait(false);

        return bookings.Select(b => b.ToTenantDto()).ToList();
    }
}
