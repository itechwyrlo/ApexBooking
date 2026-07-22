namespace ApexBooking.Core.Application.Dtos
{
    public record PublicTenantDto(
        string BusinessName,
        string? LogoUrl,
        string? ContactEmail,
        string? ContactPhone,
        string? City,
        string? CountryCode,
    string? WebsiteUrl,
    int MinAdvanceBookingHours,
    int MaxAdvanceBookingDays,
    int CancellationCutoffHours,
    string Timezone
);
}