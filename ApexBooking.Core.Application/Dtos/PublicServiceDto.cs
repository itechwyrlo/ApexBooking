namespace ApexBooking.Core.Application.Dtos
{
    public record PublicServiceDto(
        Guid ServiceId,
        string Name,
        string? Description,
        int DurationMinutes,
        decimal Price,
        string CurrencyCode
    );
}