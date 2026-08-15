using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentGateway
{
    public class SetPaymentGatewayWebhookSecretHandler : ICommandHandler<SetPaymentGatewayWebhookSecretCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public SetPaymentGatewayWebhookSecretHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(SetPaymentGatewayWebhookSecretCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to save webhook secret. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.PaymentCredential!
            );

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to save webhook secret. Isolated tenant context could not be verified.");

            tenant.SetPaymentGatewayWebhookSecret(command.WebhookSecret);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
