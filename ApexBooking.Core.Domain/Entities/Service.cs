using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class Service : IAggregateRoot, ITenantEntity
{
    public ServiceId ServiceId { get; protected set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>
    /// Duration of the service in minutes.
    /// Used by the slot calculator to divide available time into bookable slots.
    /// TR-9.1 Step 1
    /// </summary>
    public int DurationMinutes { get; private set; }

    /// <summary>
    /// Minutes to block before the scheduled start time of each booking.
    /// Applied when computing blocked windows around existing bookings.
    /// TR-9.1 Step 7
    /// </summary>
    public int BufferBeforeMinutes { get; private set; }

    /// <summary>
    /// Minutes to block after the scheduled end time of each booking.
    /// Also added to the end of each candidate slot to ensure it fits within the available window.
    /// TR-9.1 Steps 7 and 8
    /// </summary>
    public int BufferAfterMinutes { get; private set; }

    public decimal Price { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;

    /// <summary>
    /// Overrides TenantSettings.MinAdvanceBookingHours when set.
    /// TR-9.1 Step 1: load service override first, fall back to tenant settings.
    /// </summary>
    public int? MinAdvanceBookingHours { get; private set; }

    /// <summary>
    /// Overrides TenantSettings.MaxAdvanceBookingDays when set.
    /// TR-9.1 Step 1: load service override first, fall back to tenant settings.
    /// </summary>
    public int? MaxAdvanceBookingDays { get; private set; }

    public CancellationPolicy? CancellationPolicyOverride { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Children: resource assignments for this service
    private readonly List<ServiceStaff> _serviceStaffs = new();
    public IReadOnlyCollection<ServiceStaff> ServiceStaffs => _serviceStaffs.AsReadOnly();

    protected Service() { }

    private Service(
        TenantId tenantId,
        string name,
        string? description,
        int durationMinutes,
        int bufferBeforeMinutes,
        int bufferAfterMinutes,
        decimal price,
        string currencyCode,
        int? minAdvanceBookingHours,
        int? maxAdvanceBookingDays)
    {
        ServiceId = new ServiceId(Guid.NewGuid());
        TenantId = tenantId;
        Name = name;
        Description = description;
        DurationMinutes = durationMinutes;
        BufferBeforeMinutes = bufferBeforeMinutes;
        BufferAfterMinutes = bufferAfterMinutes;
        Price = price;
        CurrencyCode = currencyCode;
        MinAdvanceBookingHours = minAdvanceBookingHours;
        MaxAdvanceBookingDays = maxAdvanceBookingDays;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Service Create(
        TenantId tenantId,
        string name,
        int durationMinutes,
        decimal price,
        string currencyCode,
        IEnumerable<StaffId> staffIds,
        string? description = null,
        int bufferBeforeMinutes = 0,
        int bufferAfterMinutes = 0,
        int? minAdvanceBookingHours = null,
        int? maxAdvanceBookingDays = null)
    {
        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleBrokenException("Service name is required.");

        if (durationMinutes <= 0)
            throw new BusinessRuleBrokenException("Duration must be greater than zero.");

        if (price < 0)
            throw new BusinessRuleBrokenException("Price cannot be negative.");

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            throw new BusinessRuleBrokenException("A valid ISO 4217 currency code is required.");

        if (bufferBeforeMinutes < 0)
            throw new BusinessRuleBrokenException("Buffer before cannot be negative.");

        if (bufferAfterMinutes < 0)
            throw new BusinessRuleBrokenException("Buffer after cannot be negative.");

        var resourceList = staffIds.ToList();
        if (resourceList.Count == 0)
            throw new BusinessRuleBrokenException("At least one resource must be assigned to the service.");

        var service = new Service(tenantId, name, description, durationMinutes, bufferBeforeMinutes, bufferAfterMinutes, price, currencyCode.ToUpperInvariant(), minAdvanceBookingHours, maxAdvanceBookingDays);

        foreach (var resourceId in resourceList)
            service.AddResource(resourceId);

        return service;
    }

    public void Update(
        string name,
        string? description,
        int durationMinutes,
        decimal price,
        string currencyCode,
        int bufferBeforeMinutes,
        int bufferAfterMinutes,
        int? minAdvanceBookingHours,
        int? maxAdvanceBookingDays)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleBrokenException("Service name is required.");

        if (durationMinutes <= 0)
            throw new BusinessRuleBrokenException("Duration must be greater than zero.");

        if (price < 0)
            throw new BusinessRuleBrokenException("Price cannot be negative.");

        Name = name;
        Description = description;
        DurationMinutes = durationMinutes;
        Price = price;
        CurrencyCode = currencyCode.ToUpperInvariant();
        BufferBeforeMinutes = bufferBeforeMinutes;
        BufferAfterMinutes = bufferAfterMinutes;
        MinAdvanceBookingHours = minAdvanceBookingHours;
        MaxAdvanceBookingDays = maxAdvanceBookingDays;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleBrokenException("Service is already inactive.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            throw new BusinessRuleBrokenException("Service is already active.");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Resource Assignment Management ───────────────────────────────────────

    /// <summary>
    /// Assigns a resource to this service.
    /// TR-8.1: all resource_ids must belong to the same tenant.
    /// Duplicate assignments are silently ignored.
    /// </summary>
    public void AddResource(StaffId staffId)
    {
        if (staffId is null)
            throw new BusinessRuleBrokenException("Resource is required.");

        bool alreadyAssigned = _serviceStaffs.Any(sr => sr.StaffId == staffId);
        if (alreadyAssigned)
            return;

        var serviceResource = ServiceStaff.Create(ServiceId, staffId, TenantId);
        _serviceStaffs.Add(serviceResource);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Replaces the full set of resource assignments.
    /// TR-8.2: when resource_ids is provided on update, it replaces the full set.
    /// </summary>
    public void ReplaceResources(IEnumerable<StaffId> staffIds)
    {
        var list = staffIds.ToList();

        if (list.Count == 0)
            throw new BusinessRuleBrokenException("At least one resource must be assigned to the service.");

        _serviceStaffs.Clear();

        foreach (var staffId in list)
            AddResource(staffId);

        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveResource(StaffId staffId)
    {
        var serviceResource = _serviceStaffs.FirstOrDefault(sr => sr.StaffId == staffId);
        if (serviceResource is null)
            throw new BusinessRuleBrokenException("Resource is not assigned to this service.");

        if (_serviceStaffs.Count == 1)
            throw new BusinessRuleBrokenException("A service must have at least one resource assigned.");

        _serviceStaffs.Remove(serviceResource);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// TR-9.1 Step 3: check whether a specific resource is linked to this service.
    /// </summary>
    public bool IsStaffAssigned(StaffId staffId)
        => _serviceStaffs.Any(sr => sr.StaffId == staffId);

    public void EnsureStaffAssigned(StaffId staffId)
    {
        if (!IsStaffAssigned(staffId))
            throw new BusinessRuleBrokenException("Resource not assigned to service.");
    }

    public static void EnsureNameIsUnique(bool nameExists)
    {
        if (nameExists)
            throw new BusinessRuleBrokenException("A service with this name already exists.");
    }

    /// <summary>
    /// Returns the effective minimum advance booking hours for slot validation.
    /// Uses service-level override if set, otherwise falls back to the tenant setting.
    /// TR-9.1 Step 1.
    /// </summary>
    public int GetEffectiveMinAdvanceBookingHours(int tenantDefault)
        => MinAdvanceBookingHours ?? tenantDefault;

    /// <summary>
    /// Returns the effective maximum advance booking days for slot validation.
    /// Uses service-level override if set, otherwise falls back to the tenant setting.
    /// TR-9.1 Step 1.
    /// </summary>
    public int GetEffectiveMaxAdvanceBookingDays(int tenantDefault)
        => MaxAdvanceBookingDays ?? tenantDefault;

    public void ValidateBookingWindow(DateTime scheduledUtc, DateTime nowUtc, int tenantMinHours, int tenantMaxDays)
    {
        var minHours = GetEffectiveMinAdvanceBookingHours(tenantMinHours);
        var maxDays  = GetEffectiveMaxAdvanceBookingDays(tenantMaxDays);

        if (scheduledUtc < nowUtc.AddHours(minHours))
            throw new BusinessRuleBrokenException("Booking is too soon.");

        if (scheduledUtc > nowUtc.AddDays(maxDays))
            throw new BusinessRuleBrokenException("Booking is too far in advance.");
    }

    public void EnsureDateWithinBookingWindow(DateOnly date, DateOnly earliest, DateOnly latest)
    {
        if (date < earliest || date > latest)
            throw new BusinessRuleBrokenException("Requested date is outside the booking window.");
    }
}

