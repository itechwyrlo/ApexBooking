using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentPolicy
{
    public class GetPaymentPolicyHandler : IQueryHandler<GetPaymentPolicyQuery, PaymentPolicyDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetPaymentPolicyHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<PaymentPolicyDto> Handle(GetPaymentPolicyQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load financial constraints. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.PaymentPolicy!
            );

            if (tenant == null || tenant.PaymentPolicy == null)
                throw new BusinessRuleBrokenException("Failed to load financial constraints. Policy record not found.");

            var policy = tenant.PaymentPolicy;

            return new PaymentPolicyDto(
                policy.RequirementType,
                policy.DepositType,
                policy.DepositValue,
                policy.OnTimeRefundPercent,
                policy.LateCancellationRefundPercent,
                policy.RefundReviewDeadlineDays,
                policy.RefundEnabled
            );
        }
    }
}
