using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities
{
    public class TenantPaymentCredential : ITenantEntity
    {
        public TenantPaymentCredentialId TenantPaymentCredentialId { get; private set; }
        public TenantId TenantId { get; private set; } = default!;

        //PayMongo integration tokens
        public string SecretKey { get; private set; } = string.Empty;
        public string PublicKey { get; private set; } = string.Empty;

        // Signs incoming PayMongo webhook requests (whsk_...) — set separately from the API keys
        // above, since it's only obtainable after registering a webhook endpoint in PayMongo's
        // dashboard, which itself requires the tenant's keys to already exist.
        public string? WebhookSecret { get; private set; }

        public bool IsEnabled { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        protected TenantPaymentCredential() { }

        public TenantPaymentCredential(TenantId tenantId, string secretKey, string publicKey)
        {
            if (tenantId is null)
                throw new BusinessRuleBrokenException("Tenant identification wrapper is required.");

            UpdateCredentials(secretKey, publicKey);

            TenantPaymentCredentialId = new TenantPaymentCredentialId(Guid.NewGuid());
            TenantId = tenantId;
            IsEnabled = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateCredentials(string secretKey, string publicKey)
        {
            if (string.IsNullOrWhiteSpace(secretKey) || (!secretKey.Trim().StartsWith("sk_test_") && !secretKey.Trim().StartsWith("sk_live_")))
                throw new BusinessRuleBrokenException("A valid PayMongo Secret Key starting with 'sk_' is required.");

            if (string.IsNullOrWhiteSpace(publicKey) || (!publicKey.Trim().StartsWith("pk_test_") && !publicKey.Trim().StartsWith("pk_live_")))
                throw new BusinessRuleBrokenException("A valid PayMongo Public Key starting with 'pk_' is required.");

            SecretKey = secretKey.Trim();
            PublicKey = publicKey.Trim();
            IsEnabled = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Disable()
        {
            IsEnabled = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetWebhookSecret(string webhookSecret)
        {
            if (string.IsNullOrWhiteSpace(webhookSecret) || !webhookSecret.Trim().StartsWith("whsk_"))
                throw new BusinessRuleBrokenException("A valid PayMongo Webhook Signing Secret starting with 'whsk_' is required.");

            WebhookSecret = webhookSecret.Trim();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}