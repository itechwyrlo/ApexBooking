using System;
using System.Collections.Generic;
using System.Linq;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class Service : ITenantEntity
{
    public ServiceId ServiceId { get; protected set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DurationMinutes { get; private set; }
    public int BufferBeforeMinutes { get; private set; }
    public int BufferAfterMinutes { get; private set; }
    public decimal Price { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public CancellationPolicy? CancellationPolicyOverride { get; private set; }
    public int? MinAdvanceBookingHoursOverride { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    private readonly List<MemberService> _serviceProviders = [];
    public IReadOnlyCollection<MemberService> ServiceProviders => _serviceProviders.AsReadOnly();

    protected Service() { }

    private Service(
        TenantId tenantId,
        string name,
        string? description,
        int durationMinutes,
        int bufferBeforeMinutes,
        int bufferAfterMinutes,
        decimal price,
        string currencyCode)
    {
        ServiceId = new ServiceId(Guid.NewGuid());
        TenantId = tenantId;
        Name = name.Trim();
        Description = description?.Trim();
        DurationMinutes = durationMinutes;
        BufferBeforeMinutes = bufferBeforeMinutes;
        BufferAfterMinutes = bufferAfterMinutes;
        Price = price;
        CurrencyCode = currencyCode.ToUpperInvariant().Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Factory Entry Point ──────────────────────────────────────────────────
    public static Service Create(
        TenantId tenantId,
        string name,
        int durationMinutes,
        decimal price,
        string currencyCode,
        string? description = null,
        int bufferBeforeMinutes = 0,
        int bufferAfterMinutes = 0,
        int? minAdvanceBookingHoursOverride = null)
    {
        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant identifier is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleBrokenException("Service name is required.");

        if (durationMinutes <= 0)
            throw new BusinessRuleBrokenException("Service duration must be greater than zero minutes.");

        if (price < 0)
            throw new BusinessRuleBrokenException("Service price cannot be a negative value.");

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            throw new BusinessRuleBrokenException("A valid 3-character ISO 4217 currency code is required.");

        if (bufferBeforeMinutes < 0 || bufferAfterMinutes < 0)
            throw new BusinessRuleBrokenException("Time buffer windows cannot be negative integers.");

        if (minAdvanceBookingHoursOverride is < 0)
            throw new BusinessRuleBrokenException("Minimum advance booking hours cannot be negative.");

        var service = new Service(tenantId, name, description, durationMinutes, bufferBeforeMinutes, bufferAfterMinutes, price, currencyCode);
        service.MinAdvanceBookingHoursOverride = minAdvanceBookingHoursOverride;
        return service;
    }

    // ── Mutator State Updates ────────────────────────────────────────────────
    public void Update(
        string name,
        string? description,
        int durationMinutes,
        decimal price,
        string currencyCode,
        int bufferBeforeMinutes,
        int bufferAfterMinutes,
        int? minAdvanceBookingHoursOverride = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleBrokenException("Service name is required.");

        if (durationMinutes <= 0)
            throw new BusinessRuleBrokenException("Service duration must be greater than zero minutes.");

        if (price < 0)
            throw new BusinessRuleBrokenException("Service price cannot be negative.");

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            throw new BusinessRuleBrokenException("A valid 3-character ISO 4217 currency code is required.");

        if (minAdvanceBookingHoursOverride is < 0)
            throw new BusinessRuleBrokenException("Minimum advance booking hours cannot be negative.");

        Name = name.Trim();
        Description = description?.Trim();
        DurationMinutes = durationMinutes;
        Price = price;
        CurrencyCode = currencyCode.ToUpperInvariant().Trim();
        BufferBeforeMinutes = bufferBeforeMinutes;
        BufferAfterMinutes = bufferAfterMinutes;
        MinAdvanceBookingHoursOverride = minAdvanceBookingHoursOverride;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Activation State Machine Controls ─────────────────────────────────────
    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleBrokenException("The service catalog item is already deactivated.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            throw new BusinessRuleBrokenException("The service catalog item is already active.");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Staff Assignment Controls ─────────────────────────────────────────────
    public void AssignStaffMember(MemberService provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        // Enforce business invariant: Prevent duplicate cross-links
        if (_serviceProviders.Any(p => p.TenantMemberId == provider.TenantMemberId))
            throw new BusinessRuleBrokenException("This team member is already assigned as a qualified provider for this service.");

        _serviceProviders.Add(provider);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveStaffMember(TenantMemberId tenantMemberId)
    {
        var provider = _serviceProviders.FirstOrDefault(p => p.TenantMemberId == tenantMemberId);
        if (provider == null)
            throw new BusinessRuleBrokenException("This team member is not currently assigned as a qualified provider for this service.");

        _serviceProviders.Remove(provider);
        UpdatedAt = DateTime.UtcNow;
    }
}
