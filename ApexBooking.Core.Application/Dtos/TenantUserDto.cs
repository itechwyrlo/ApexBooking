using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.Dtos;

public sealed record TenantUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string Status
);
