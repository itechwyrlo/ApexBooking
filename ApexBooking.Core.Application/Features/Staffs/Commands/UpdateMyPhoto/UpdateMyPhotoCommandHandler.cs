using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.UpdateMyPhoto
{
    internal sealed class UpdateMyPhotoCommandHandler : ICommandHandler<UpdateMyPhotoCommand, StaffDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public UpdateMyPhotoCommandHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<StaffDto> Handle(UpdateMyPhotoCommand command, CancellationToken cancellationToken)
        {
            var userId = _contextService.GetCurrentUserId();

            var staff = await _unitOfWork.StaffRepository
                .FindByUserIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            if (staff is null)
                throw new NotFoundException("Staff profile not found.");

            staff.UpdatePhoto(command.PhotoUrl);

            _unitOfWork.StaffRepository.Update(staff);
            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            return staff.ToStaffDto();
        }
    }
}
