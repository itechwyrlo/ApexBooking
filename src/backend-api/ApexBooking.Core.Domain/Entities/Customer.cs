using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class Customer : IAggregateRoot, IHasDomainEvents, ITenantEntity
{
    public CustomerId CustomerId { get; protected set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public CustomerContact Contact { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected Customer() { }

    private Customer(TenantId tenantId, CustomerContact contact)
    {
        CustomerId = new CustomerId(Guid.NewGuid());
        TenantId = tenantId;
        Contact = contact;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CustomerCreatedDomainEvent(CustomerId));
    }

    public static Customer Create(TenantId tenantId, CustomerContact contact)
    {
        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant is required.");

        if (contact is null)
            throw new BusinessRuleBrokenException("Customer contact is required.");

        return new Customer(tenantId, contact);
    }

    public void UpdateContact(CustomerContact contact)
    {
        Contact = contact ?? throw new BusinessRuleBrokenException("Customer contact is required.");
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CustomerUpdatedDomainEvent(CustomerId));
    }
}
