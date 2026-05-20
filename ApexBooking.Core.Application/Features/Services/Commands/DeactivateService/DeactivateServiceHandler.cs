using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Services.Commands.DeactivateService
{
    internal sealed class DeactivateServiceHandler : ICommandHandler<DeactivateServiceCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public DeactivateServiceHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task Handle(DeactivateServiceCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var serviceId = new ServiceId(command.ServiceId);

            var service = await _unitOfWork.ServiceRepository
                .GetByIdAsync(serviceId)
                .ConfigureAwait(false);

            if (service is null || service.TenantId != tenantId)
                throw new NotFoundException("Service not found.");

            service.Deactivate();

            _unitOfWork.ServiceRepository.Update(service);
            await _unitOfWork.CompleteAsync(ct);
        }
    }
}