using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Availability.Commands.CreateResource
{
    public class CreateResourceHandler : ICommandHandler<CreateResourceCommand, BaseResponse<Resource>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public CreateResourceHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<Resource>> Handle(CreateResourceCommand command, CancellationToken ct)
        {
            var resource = Resource.Create(
                tenantId: _contextService.GetCurrentTenantId(),
                name: command.Name,
                resourceType: command.ResourceType,
                capacity: command.Capacity,
                description: command.Description
            );

             _unitOfWork.ResourceRepository.Add(resource);
            await _unitOfWork.CompleteAsync(ct);
            return BaseResponse<Resource>.Success(resource);
        }
    }
}