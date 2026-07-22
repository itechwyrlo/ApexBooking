namespace ApexBooking.Core.Application.Dtos
{
    public sealed record AccountVerificationResponseDto
    {
        public string Url { get; init; }
        public string? TenantSlug { get; init; }

    }
}