namespace ApexBooking.Core.Application.Dtos
{
    public sealed record ResourceDto(
        Guid Id,
        string Name,
        string? Description,
        string ResourceType,
        int Capacity,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

}
