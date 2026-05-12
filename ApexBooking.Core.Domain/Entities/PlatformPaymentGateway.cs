using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Domain.Entities;

public class PlatformPaymentGateway : IAggregateRoot
{
    public PlatformPaymentGatewayId PlatformPaymentGatewayId { get; protected set; } = default!;
    public GatewayProvider GatewayProvider { get; private set; }
    public string ClientId { get; private set; } = string.Empty;
    public string SecretKeyEncrypted { get; private set; } = string.Empty;
    public string WebhookId { get; private set; } = string.Empty;
    public GatewayMode Mode { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? ValidatedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected PlatformPaymentGateway() { }

    private PlatformPaymentGateway(
        GatewayProvider gatewayProvider,
        string clientId,
        string secretKeyEncrypted,
        string webhookId,
        GatewayMode mode)
    {
        PlatformPaymentGatewayId = new PlatformPaymentGatewayId(Guid.NewGuid());
        GatewayProvider = gatewayProvider;
        ClientId = clientId;
        SecretKeyEncrypted = secretKeyEncrypted;
        WebhookId = webhookId;
        Mode = mode;
        IsActive = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static PlatformPaymentGateway Create(
        GatewayProvider gatewayProvider,
        string clientId,
        string secretKeyEncrypted,
        string webhookId,
        GatewayMode mode)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new BusinessRuleBrokenException("Client ID is required.");

        if (string.IsNullOrWhiteSpace(secretKeyEncrypted))
            throw new BusinessRuleBrokenException("Secret key is required.");

        if (string.IsNullOrWhiteSpace(webhookId))
            throw new BusinessRuleBrokenException("Webhook ID is required.");

        return new PlatformPaymentGateway(gatewayProvider, clientId, secretKeyEncrypted, webhookId, mode);
    }

    public void MarkValidated()
    {
        IsActive = true;
        ValidatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
