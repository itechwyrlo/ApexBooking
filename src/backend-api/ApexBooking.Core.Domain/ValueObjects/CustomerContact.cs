using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.ValueObjects;

/// <summary>
/// A customer's contact details (05 §5). Name is required; Email and PhoneNumber are each
/// optional but at least one must be present — a customer with no way to reach them can't
/// receive a booking confirmation or reminder (product §13).
/// </summary>
public record CustomerContact
{
    public string Name { get; }
    public string? Email { get; }
    public string? PhoneNumber { get; }

    private CustomerContact() : this("placeholder", "placeholder@example.com", null, skipValidation: true) { }

    public CustomerContact(string name, string? email, string? phoneNumber)
        : this(name, email, phoneNumber, skipValidation: false) { }

    private CustomerContact(string name, string? email, string? phoneNumber, bool skipValidation)
    {
        if (!skipValidation)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessRuleBrokenException("Customer name is required.");

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber))
                throw new BusinessRuleBrokenException("A customer needs at least an email or a phone number.");
        }

        Name = name;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
    }
}
