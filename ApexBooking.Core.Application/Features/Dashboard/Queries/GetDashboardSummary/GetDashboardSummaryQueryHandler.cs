using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Dashboard.Queries.GetDashboardSummary
{
    internal sealed class GetDashboardSummaryQueryHandler
        : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetDashboardSummaryQueryHandler(
            IUnitOfWork unitOfWork,
            IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<DashboardSummaryDto> Handle(
            GetDashboardSummaryQuery query,
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

                var staffBookings = await _unitOfWork.BookingRepository.GetAllAsync(
                    b => b.TenantId == tenantId && b.StaffId == staff.StaffId);
                return staffBookings.ToDashboardSummaryDto();
            }

            var bookings = await _unitOfWork.BookingRepository.GetAllAsync(
                b => b.TenantId == tenantId);
            return bookings.ToDashboardSummaryDto();
        }
    }
}
