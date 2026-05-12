using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.Dtos;

public sealed record OrganizationSummaryDto(
    Guid Id,
    string Slug,
    string BusinessName,
    string OwnerEmail,
    string Status,
    int UserCount,
    DateTime CreatedAt
);
