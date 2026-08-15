using System.Runtime.CompilerServices;

// Grants ApexBooking.Core.Domain.UnitTests access to this assembly's `internal` members —
// e.g. Booking.ClearPendingPaymentOnArrival(), which is deliberately internal (not part of
// the domain's public API) but still needs direct unit-test coverage. Purely a compile-time
// visibility grant to the named test assembly; it does not change what's visible to
// Core.Application, Infrastructure, WebApi, or any other consumer, and has no runtime effect.
[assembly: InternalsVisibleTo("ApexBooking.Core.Domain.UnitTests")]
