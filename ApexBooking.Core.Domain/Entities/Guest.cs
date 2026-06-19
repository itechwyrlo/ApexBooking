using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class Guest : ITenantEntity
{
    public GuestId GuestId { get; private set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public BookingId BookingId { get; private set; } = default!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public GuestCancellationToken? CancellationToken { get; private set; }
    public Booking? Booking { get; private set; }

    protected Guest() { }

    private Guest(
        TenantId tenantId,
        BookingId bookingId,
        string firstName,
        string lastName,
        string email,
        string? phone)
    {
        GuestId = new GuestId(Guid.NewGuid());
        TenantId = tenantId;
        BookingId = bookingId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        CreatedAt = DateTime.UtcNow;
    }

    public static Guest Create(
        TenantId tenantId,
        BookingId bookingId,
        string firstName,
        string lastName,
        string email,
        string? phone)
    {
        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant is required.");
        if (bookingId is null)
            throw new BusinessRuleBrokenException("Booking is required.");
        if (string.IsNullOrWhiteSpace(firstName))
            throw new BusinessRuleBrokenException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new BusinessRuleBrokenException("Last name is required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleBrokenException("Email is required.");

        return new Guest(
            tenantId,
            bookingId,
            firstName.Trim(),
            lastName.Trim(),
            email.Trim().ToLowerInvariant(),
            phone?.Trim());
    }

    public string FullName => $"{FirstName} {LastName}";
}
