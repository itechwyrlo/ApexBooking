using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Services.Ticketing
{
    public readonly record struct TicketPayload(BookingId BookingId, TenantId TenantId, BranchId BranchId);

    public interface ITicketTokenService
    {
        string Issue(TicketPayload payload);
        bool TryValidate(string token, out TicketPayload payload);
    }
}
