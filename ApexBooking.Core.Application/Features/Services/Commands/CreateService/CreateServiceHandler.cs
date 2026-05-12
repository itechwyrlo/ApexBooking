using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Features.service;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Application.Services.Mappings;
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

            var tenant = await _unitOfWork.TenantRepository.GetAsync(t => t.TenantId == tenantId);
            if (tenant is null)
                return BaseResponse<ServiceDto>.Failure("Tenant not found.");

            var limit = PlanLimits.MaxServices(tenant.Plan);
            if (limit.HasValue)
            {
                var existing = await _unitOfWork.ServiceRepository.GetAllAsync();
                if (existing.Count() >= limit.Value)
                    throw new BusinessRuleBrokenException(
                        $"Your {tenant.Plan} plan allows a maximum of {limit.Value} services.");
            }

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

            return BaseResponse<ServiceDto>.Success(service.ToServiceDto());
        }
    }
}
