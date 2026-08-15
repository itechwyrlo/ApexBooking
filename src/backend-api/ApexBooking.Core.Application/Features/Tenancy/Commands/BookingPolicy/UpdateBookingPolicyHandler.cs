using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.BookingPolicy
{
    public class UpdateBookingPolicyHandler : ICommandHandler<UpdateBookingPolicyCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public UpdateBookingPolicyHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(UpdateBookingPolicyCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to update scheduling rules. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.BookingPolicy!
            );

            if (tenant == null || tenant.BookingPolicy == null)
                throw new BusinessRuleBrokenException("Failed to update scheduling rules. Policy record not found.");

            tenant.BookingPolicy.UpdateSettings(
                command.BookingConfirmationMode,
                command.MinAdvanceBookingHours,
                command.MaxAdvanceBookingDays,
                command.CancellationCutoffHours,
                command.LateCancellationPolicy,
                command.NotifyBookingConfirmed,
                command.NotifyBookingCancelled,
                command.NotifyBookingReminder,
                command.NotifyNewCustomer,
                command.ReminderHoursBefore
            );

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}