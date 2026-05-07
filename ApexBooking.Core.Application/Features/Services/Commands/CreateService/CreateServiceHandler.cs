using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Features.service;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Services.Commands.CreateService
{
    internal sealed class CreateServiceHandler : ICommandHandler<CreateServiceCommand, BaseResponse<ServiceDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public CreateServiceHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<ServiceDto>> Handle(CreateServiceCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();

            bool nameExists = await _unitOfWork.ServiceRepository.NameExistsAsync(command.Name);
            if (nameExists)
                return BaseResponse<ServiceDto>.Failure("A service with this name already exists.");

            var resourceIds = command.ResourceIds.Select(id => new ResourceId(id)).ToList();

            var service = Service.Create(
                tenantId,
                command.Name,
                command.DurationMinutes,
                command.Price,
                command.CurrencyCode,
                resourceIds,
                command.Description,
                command.BufferBeforeMinutes,
                command.BufferAfterMinutes,
                command.MinAdvanceBookingHours,
                command.MaxAdvanceBookingDays
            );

            _unitOfWork.ServiceRepository.Add(service);
            await _unitOfWork.CompleteAsync(ct);

            return BaseResponse<ServiceDto>.Success(MapToDto(service));
        }

        private static ServiceDto MapToDto(Service service) => new()
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