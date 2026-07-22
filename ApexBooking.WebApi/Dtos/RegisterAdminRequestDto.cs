namespace ApexBooking.WebApi.Dtos
{
    public record RegisterAdminRequestDto(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string OrganizationName,
        string Industry,
        string Phone,
        string Country);
}