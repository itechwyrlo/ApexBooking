using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
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

            var service = await _unitOfWork.ServiceRepository
                .FindByIdWithResourcesAsync(serviceId, ct)
                .ConfigureAwait(false);

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

            return BaseResponse<ServiceDto>.Success(MapToDto(service));
        }

        private static ServiceDto MapToDto(Core.Domain.Entities.Service service) => new()
        {
            Id = service.ServiceId.Value,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            BufferBeforeMinutes = service.BufferBeforeMinutes,
            BufferAfterMinutes = service.BufferAfterMinutes,
            Price = service.Price,
            CurrencyCode = service.CurrencyCode,
            MinAdvanceBookingHours = service.MinAdvanceBookingHours,
            MaxAdvanceBookingDays = service.MaxAdvanceBookingDays,
            IsActive = service.IsActive,
            ResourceIds = service.ServiceResources.Select(sr => sr.ResourceId.Value).ToList(),
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt
        };
    }
}