using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Availability.Commands.RemoveException
{
    internal sealed class RemoveExceptionHandler : ICommandHandler<RemoveExceptionCommand, BaseResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public RemoveExceptionHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<bool>> Handle(RemoveExceptionCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var staffId = new StaffId(command.StaffId);

            var staff = await _unitOfWork.StaffRepository
                .FindByIdWithAvailabilityAsync(staffId, ct)
                .ConfigureAwait(false);

            if (staffId is null || staff.TenantId != tenantId)
                throw new BusinessRuleBrokenException("StaffId not found.");

            var exceptionId = new StaffAvailabilityExceptionId(command.ExceptionId);
            staff.RemoveException(exceptionId);

            _unitOfWork.StaffRepository.Update(staff);
            await _unitOfWork.CompleteAsync(ct);

            return BaseResponse<bool>.Success(true);
        }
    }
}