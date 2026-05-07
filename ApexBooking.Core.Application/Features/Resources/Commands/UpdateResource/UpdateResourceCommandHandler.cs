using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Resources.Commands.UpdateResource
{
    internal sealed class UpdateResourceCommandHandler : ICommandHandler<UpdateResourceCommand, BaseResponse<ResourceId>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public UpdateResourceCommandHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<ResourceId>> Handle(UpdateResourceCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var resourceId = new ResourceId(command.ResourceId);

            var resource = await _unitOfWork.ResourceRepository
                .GetByIdAsync(resourceId)
                .ConfigureAwait(false);

            if (resource is null || resource.TenantId != tenantId)
                return BaseResponse<ResourceId>.Failure("Resource not found.");

            resource.Update(command.Name, command.Description, command.Capacity);

            _unitOfWork.ResourceRepository.Update(resource);
            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            return BaseResponse<ResourceId>.Success(resourceId);
        }
    }
}