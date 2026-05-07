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
            var resourceId = new ResourceId(command.ResourceId);

            var resource = await _unitOfWork.ResourceRepository
                .FindByIdWithAvailabilityAsync(resourceId, ct)
                .ConfigureAwait(false);

            if (resource is null || resource.TenantId != tenantId)
                throw new BusinessRuleBrokenException("Resource not found.");

            var exceptionId = new ResourceAvailabilityExceptionId(command.ExceptionId);
            resource.RemoveException(exceptionId);

            _unitOfWork.ResourceRepository.Update(resource);
            await _unitOfWork.CompleteAsync(ct);

            return BaseResponse<bool>.Success(true);
        }
    }
}