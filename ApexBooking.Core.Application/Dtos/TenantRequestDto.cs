namespace ApexBooking.Core.Application.Dtos;

public sealed record TenantRequestDto(
    Guid Id,
    string BusinessName,
    string OwnerFullName,
    string OwnerEmail,
    string Plan,
    string Status,
    DateTime CreatedAt
);

public sealed record TenantRequestDetailDto(
    Guid Id,
    string BusinessName,
    string OwnerFullName,
    string OwnerEmail,
    string OwnerPhone,
    string Plan,
    string? Message,
    string Status,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ReviewedAt
);
