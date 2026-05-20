using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Features.service;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Services.Mappings;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Services.Commands.CreateService
{
    internal sealed class CreateServiceHandler : ICommandHandler<CreateServiceCommand, ServiceDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public CreateServiceHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<ServiceDto> Handle(CreateServiceCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();

            var tenant = await _unitOfWork.TenantRepository.GetAsync(t => t.TenantId == tenantId);
            if (tenant is null)
                throw new NotFoundException("Tenant not found.");

            var existing = await _unitOfWork.ServiceRepository.GetAllAsync();
            tenant.EnforcePlanServiceLimit(existing.Count());

            bool nameExists = await _unitOfWork.ServiceRepository.NameExistsAsync(command.Name);
            Service.EnsureNameIsUnique(nameExists);

            var staffIds = command.StaffIds.Select(id => new StaffId(id)).ToList();

            var service = Service.Create(
                tenantId,
                command.Name,
                command.DurationMinutes,
                command.Price,
                command.CurrencyCode,
                staffIds,
                command.Description,
                command.BufferBeforeMinutes,
                command.BufferAfterMinutes,
                command.MinAdvanceBookingHours,
                command.MaxAdvanceBookingDays
            );

            _unitOfWork.ServiceRepository.Add(service);
            await _unitOfWork.CompleteAsync(ct);

            return service.ToServiceDto();
        }
    }
}
