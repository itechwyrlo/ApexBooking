using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class TenantPaymentAccount
{
    public TenantPaymentAccountId Id { get; private set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public TenantPaymentAccountStatus Status { get; private set; }
    public string? ProviderAccountReference { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected TenantPaymentAccount() { }

    private TenantPaymentAccount(TenantId tenantId)
    {
        Id = new TenantPaymentAccountId(Guid.NewGuid());
        TenantId = tenantId;
        Status = TenantPaymentAccountStatus.Disconnected;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static TenantPaymentAccount Create(TenantId tenantId)
    {
        if (tenantId is null) throw new BusinessRuleBrokenException("Tenant is required.");
        return new TenantPaymentAccount(tenantId);
    }

    public void Connect(string providerAccountReference)
    {
        if (string.IsNullOrWhiteSpace(providerAccountReference))
            throw new BusinessRuleBrokenException("A provider account reference is required to connect.");
        ProviderAccountReference = providerAccountReference;
        Status = TenantPaymentAccountStatus.Connected;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disconnect()
    {
        ProviderAccountReference = null;
        Status = TenantPaymentAccountStatus.Disconnected;
        UpdatedAt = DateTime.UtcNow;
    }
}
