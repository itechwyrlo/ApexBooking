namespace ApexBooking.Core.Application.Dtos.Response
{
    public record CustomerSummary(
        Guid CustomerId,
        string Name,
        string? Email,
        string? PhoneNumber,
        DateTime CreatedAt
    );
}
