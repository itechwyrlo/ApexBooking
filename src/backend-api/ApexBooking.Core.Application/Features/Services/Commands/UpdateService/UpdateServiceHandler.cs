using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Services.Commands.UpdateService
{
    public class UpdateServiceHandler : ICommandHandler<UpdateServiceCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public UpdateServiceHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(UpdateServiceCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to update service catalog entry. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Services
            );

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to update service catalog entry. Isolated tenant context could not be verified.");


            tenant.UpdateService(
                serviceId: command.ServiceId,
                name: command.Name,
                description: command.Description,
                durationMinutes: command.DurationMinutes,
                price: command.Price,
                currencyCode: command.CurrencyCode,
                bufferBeforeMinutes: command.BufferBeforeMinutes,
                bufferAfterMinutes: command.BufferAfterMinutes,
                minAdvanceBookingHoursOverride: command.MinAdvanceBookingHoursOverride
            );

            _unitOfWork.TenantRepository.Update(tenant);

            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}