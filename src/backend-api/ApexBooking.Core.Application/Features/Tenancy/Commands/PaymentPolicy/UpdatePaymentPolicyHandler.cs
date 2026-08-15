using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentPolicy
{
    public class UpdatePaymentPolicyHandler : ICommandHandler<UpdatePaymentPolicyCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public UpdatePaymentPolicyHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(UpdatePaymentPolicyCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to update financial constraints. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.PaymentPolicy!
            );

            if (tenant == null || tenant.PaymentPolicy == null)
                throw new BusinessRuleBrokenException("Failed to update financial constraints. Policy record not found.");

            tenant.PaymentPolicy.UpdatePolicy(
                command.RequirementType,
                command.DepositType,
                command.DepositValue,
                command.OnTimeRefundPercent,
                command.LateCancellationRefundPercent,
                command.RefundReviewDeadlineDays,
                command.RefundEnabled
            );

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}