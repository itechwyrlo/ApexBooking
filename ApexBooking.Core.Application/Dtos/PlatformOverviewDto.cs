namespace ApexBooking.Core.Application.Dtos;

public sealed record PlatformOverviewDto(
    int TotalOrgs,
    int ActiveOrgs,
    int InactiveOrgs,
    IEnumerable<OrganizationSummaryDto> Organizations
);
