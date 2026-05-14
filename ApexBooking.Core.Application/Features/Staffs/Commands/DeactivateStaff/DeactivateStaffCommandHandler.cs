using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.DeactivateStaff
{
    internal sealed class DeactivateStaffCommandHandler : ICommandHandler<DeactivateStaffCommand, BaseResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public DeactivateStaffCommandHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<bool>> Handle(DeactivateStaffCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var staffId = new StaffId(command.staffId);

            var staff = await _unitOfWork.StaffRepository
                .GetByIdAsync(staffId)
                .ConfigureAwait(false);

            if (staff is null || staff.TenantId != tenantId)
                return BaseResponse<bool>.Failure("Resource not found.");

            staff.Deactivate();

            _unitOfWork.StaffRepository.Update(staff);
            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            
            return BaseResponse<bool>.Success(true);
        }
    }
}