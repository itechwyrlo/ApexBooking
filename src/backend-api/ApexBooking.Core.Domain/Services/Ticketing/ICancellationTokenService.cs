using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services.Ticketing
{
    public readonly record struct CancellationTokenPayload(BookingId BookingId, TenantId TenantId);

    // Deliberately independent from ITicketTokenService (the admission QR credential) — a
    // customer's admission ticket must never double as a way to cancel someone's booking, and
    // vice versa. Same deterministic-HMAC shape, different signing key, different payload.
    public interface ICancellationTokenService
    {
        string Issue(CancellationTokenPayload payload);
        bool TryValidate(string token, out CancellationTokenPayload payload);
    }
}
