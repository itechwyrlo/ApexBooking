using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.UpdateStaff
{
    internal sealed class UpdateStaffCommandHandler : ICommandHandler<UpdateStaffCommand, StaffDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public UpdateStaffCommandHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<StaffDto> Handle(UpdateStaffCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var staffId = new StaffId(command.StaffId);

            var staff = await _unitOfWork.StaffRepository
                .GetByIdAsync(staffId)
                .ConfigureAwait(false);

            if (staff is null || staff.TenantId != tenantId)
                throw new NotFoundException("Staff not found.");

            staff.Update(command.email, command.contactNumber, command.Description, command.Capacity);

            _unitOfWork.StaffRepository.Update(staff);
            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            return staff.ToStaffDto();
        }
    }
}