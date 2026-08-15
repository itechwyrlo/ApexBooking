using System;

namespace ApexBooking.Core.Application.Dtos.Response
{
    public record ScanArrivalResult(
        Guid BookingId,
        string BookingReference,
        string Status,
        DateTime CheckedInAt,
        bool WasFirstAdmission
    );
}
