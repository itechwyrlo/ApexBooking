using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Resources.Commands.DeactivateResource
{
    internal sealed class DeactivateResourceCommandHandler : ICommandHandler<DeactivateResourceCommand, BaseResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public DeactivateResourceCommandHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<bool>> Handle(DeactivateResourceCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var resourceId = new ResourceId(command.ResourceId);

            var resource = await _unitOfWork.ResourceRepository
                .GetByIdAsync(resourceId)
                .ConfigureAwait(false);

            if (resource is null || resource.TenantId != tenantId)
                return BaseResponse<bool>.Failure("Resource not found.");

            resource.Deactivate();

            _unitOfWork.ResourceRepository.Update(resource);
            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            
            return BaseResponse<bool>.Success(true);
        }
    }
}