namespace ApexBooking.WebApi.Dtos
{
    public record GuestCancelRequestDto(string Token, string? Reason);
}
