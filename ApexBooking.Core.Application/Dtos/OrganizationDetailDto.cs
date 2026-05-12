namespace ApexBooking.Core.Application.Dtos;

public sealed record OrganizationDetailDto(
    Guid Id,
    string Slug,
    string BusinessName,
    string OwnerFullName,
    string OwnerEmail,
    string OwnerPhone,
    string Status,
    int BookingCount,
    int ServiceCount,
    int StaffCount,
    int ClientCount,
    int UserCount,
    DateTime CreatedAt,
    IEnumerable<TenantUserDto> Users
);
