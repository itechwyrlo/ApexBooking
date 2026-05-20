using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetMyProfile
{
    internal sealed class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, StaffDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetMyProfileQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<StaffDto> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
        {
            var userId = _contextService.GetCurrentUserId();

            var staff = await _unitOfWork.StaffRepository
                .FindByUserIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            if (staff is null)
                throw new NotFoundException("Staff profile not found.");

            return staff.ToStaffDto();
        }
    }
}
