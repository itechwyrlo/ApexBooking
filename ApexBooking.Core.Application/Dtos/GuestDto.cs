namespace ApexBooking.Core.Application.Dtos
{
    public sealed record GuestDto(
        Guid GuestId,
        string FirstName,
        string LastName,
        string Email,
        string? Phone
    );
}
