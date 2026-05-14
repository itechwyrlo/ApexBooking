using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.mapper
{
    public static class BookingMappings
    {
        public static GuestDto? ToGuestDto(this Guest? guest) =>
            guest is null ? null : new GuestDto(
                guest.GuestId.Value,
                guest.FirstName,
                guest.LastName,
                guest.Email,
                guest.Phone);

        public static BookingDetailDto ToDetailDto(
            this Booking booking,
            string serviceName,
            string staffName) => new(
                booking.BookingId.Value,
                booking.BookingReference,
                booking.ServiceId.Value,
                serviceName,
                booking.StaffId.Value,
                staffName,
                booking.Guest.ToGuestDto(),
                booking.ScheduledDate,
                booking.ScheduledStartTime,
                booking.ScheduledEndTime,
                booking.DurationMinutes,
                booking.Status,
                booking.ConfirmationMode,
                booking.PriceSnapshot,
                booking.CurrencyCode,
                booking.CustomerNotes,
                booking.CancellationReason,
                booking.CancelledAt,
                booking.CreatedAt);

        public static CancellationTokenValidationDto ToCancellationValidationDto(
            this Booking booking,
            Guest guest,
            TenantPaymentPolicy? policy) => new(
                booking.BookingId.Value,
                booking.BookingReference,
                booking.ServiceName,
                booking.ResourceName,
                booking.ScheduledDate,
                booking.ScheduledStartTime,
                booking.Status,
                guest.FirstName,
                guest.LastName,
                booking.PriceSnapshot,
                booking.CurrencyCode,
                policy?.RefundPercent ?? 0m,
                policy?.DepositOnly ?? false,
                policy?.DepositType ?? DepositType.Percentage,
                policy?.DepositValue ?? 0m);

        public static TenantBookingsDto ToTenantDto(this Booking booking) => new(
            booking.BookingId.Value,
            booking.BookingReference,
            booking.ServiceId.Value,
            booking.ServiceName,
            booking.StaffId.Value,
            booking.ResourceName,
            booking.Guest.ToGuestDto(),
            booking.ScheduledDate,
            booking.ScheduledStartTime,
            booking.ScheduledEndTime,
            booking.DurationMinutes,
            booking.Status,
            booking.ConfirmationMode,
            booking.PriceSnapshot,
            booking.CurrencyCode,
            booking.CustomerNotes,
            booking.CancellationReason,
            booking.CancelledAt,
            booking.CreatedAt);

            public static PublicBookingDto ToPublicDto(this Booking booking) => new(
                booking.BookingId.Value,
                booking.BookingReference,
                booking.ServiceName,
                booking.ResourceName,
                booking.ScheduledDate,
                booking.ScheduledStartTime,
                booking.ScheduledEndTime,
                booking.Status,
                booking.Guest.FirstName);
    }

    
}
