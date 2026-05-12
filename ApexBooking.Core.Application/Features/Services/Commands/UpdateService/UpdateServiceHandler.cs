using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Services.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Services.Commands.UpdateService
{
    internal sealed class UpdateServiceHandler : ICommandHandler<UpdateServiceCommand, BaseResponse<ServiceDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public UpdateServiceHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<ServiceDto>> Handle(UpdateServiceCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var serviceId = new ServiceId(command.ServiceId);

            var service = await _unitOfWork.ServiceRepository.GetAsync(
                predicate: s => s.ServiceId == serviceId,
                includes: s => s.ServiceResources).ConfigureAwait(false);

            if (service is null || service.TenantId != tenantId)
                return BaseResponse<ServiceDto>.Failure("Service not found.");

            service.Update(
                command.Name,
                command.Description,
                command.DurationMinutes,
                command.Price,
                command.CurrencyCode,
                command.BufferBeforeMinutes,
                command.BufferAfterMinutes,
                command.MinAdvanceBookingHours,
                command.MaxAdvanceBookingDays
            );

            var resourceIds = command.ResourceIds.Select(id => new ResourceId(id)).ToList();
            service.ReplaceResources(resourceIds);

            _unitOfWork.ServiceRepository.Update(service);
            await _unitOfWork.CompleteAsync(ct);

            return BaseResponse<ServiceDto>.Success(service.ToServiceDto());
        }
    }
}