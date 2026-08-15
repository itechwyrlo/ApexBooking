using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentGatewayCredential
{
    public class GetPaymentGatewayCredentialHandler : IQueryHandler<GetPaymentGatewayCredentialQuery, PaymentGatewayCredentialDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetPaymentGatewayCredentialHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<PaymentGatewayCredentialDto> Handle(GetPaymentGatewayCredentialQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load payment gateway settings. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.PaymentCredential!
            );

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to load payment gateway settings. Isolated tenant context could not be verified.");

            var credential = tenant.PaymentCredential;

            // No credential configured yet is a valid state, unlike PaymentPolicy which is
            // seeded on tenant creation — don't throw, just report "not connected."
            if (credential == null)
                return new PaymentGatewayCredentialDto(null, null, null, false, false);

            var mode = credential.SecretKey.StartsWith("sk_live_") ? "Live" : "Test";
            var maskedSecretKey = MaskSecretKey(credential.SecretKey);

            return new PaymentGatewayCredentialDto(credential.PublicKey, maskedSecretKey, mode, true, credential.WebhookSecret != null);
        }

        private static string MaskSecretKey(string secretKey)
        {
            const int prefixLength = 8; // "sk_test_" / "sk_live_"
            const int suffixLength = 4;

            if (secretKey.Length <= prefixLength + suffixLength)
                return "••••";

            var prefix = secretKey[..prefixLength];
            var suffix = secretKey[^suffixLength..];
            return $"{prefix}••••{suffix}";
        }
    }
}
