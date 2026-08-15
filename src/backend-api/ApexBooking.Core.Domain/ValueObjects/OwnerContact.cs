using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.ValueObjects;
public record OwnerContact
{
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }
    public string? PhoneNumber { get; }

    // Computed property for convenience
    public string FullName => $"{FirstName} {LastName}".Trim();

    private OwnerContact() : this(string.Empty, string.Empty, string.Empty, null, skipValidation: true) { }

    public OwnerContact(string firstName, string lastName, string email, string? phoneNumber = null)
        : this(firstName, lastName, email, phoneNumber, skipValidation: false) { }

    private OwnerContact(string firstName, string lastName, string email, string? phoneNumber, bool skipValidation)
    {
        if (!skipValidation)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new BusinessRuleBrokenException("Owner first name is required.");

            if (string.IsNullOrWhiteSpace(lastName))
                throw new BusinessRuleBrokenException("Owner last name is required.");

            if (string.IsNullOrWhiteSpace(email))
                throw new BusinessRuleBrokenException("Owner email is required.");
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.ToLower().Trim();
        PhoneNumber = phoneNumber?.Trim();
    }
}
