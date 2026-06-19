namespace ApexBooking.WebApi.Dtos
{
    public record CreateBookingRequestDto(
        string TenantSlug,
        Guid ServiceId,
        Guid? ResourceId,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        string GuestFirstName,
        string GuestLastName,
        string GuestEmail,
        string? GuestPhone,
        string? CustomerNotes
    );
}
