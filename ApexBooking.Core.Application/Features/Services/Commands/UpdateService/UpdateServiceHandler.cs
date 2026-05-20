using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Services.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Services.Commands.UpdateService
{
    internal sealed class UpdateServiceHandler : ICommandHandler<UpdateServiceCommand, ServiceDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public UpdateServiceHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<ServiceDto> Handle(UpdateServiceCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var serviceId = new ServiceId(command.ServiceId);

            var service = await _unitOfWork.ServiceRepository.GetAsync(
                predicate: s => s.ServiceId == serviceId,
                includes: s => s.ServiceStaffs).ConfigureAwait(false);

            if (service is null || service.TenantId != tenantId)
                throw new NotFoundException("Service not found.");

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

            var staffIds = command.StaffIds.Select(id => new StaffId(id)).ToList();
            if (staffIds.Count > 0) service.ReplaceResources(staffIds);

            _unitOfWork.ServiceRepository.Update(service);
            await _unitOfWork.CompleteAsync(ct);

            return service.ToServiceDto();
        }
    }
}