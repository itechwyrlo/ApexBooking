namespace ApexBooking.Core.Application.Dtos
{
    public sealed record StaffDto(
        Guid Id,
        string firstName,
        string lastName,
        string email,
        string contactNumber,
        string? Description,
        int Capacity,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

}
