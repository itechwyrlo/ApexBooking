namespace ApexBooking.Core.Application.Dtos.Response
{
    public record MyProfileDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string FullName,
        string? PhoneNumber,
        string? PhotoUrl,
        bool IsPlatformAdmin
    )
    {
        public MyProfileDto() : this(default, string.Empty, string.Empty, string.Empty, string.Empty, default, default, default) { }
    }
}
