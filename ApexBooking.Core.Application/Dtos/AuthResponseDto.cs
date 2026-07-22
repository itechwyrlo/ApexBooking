using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record AuthResponseDto
    {
        public string AccessToken { get; init; }
        public string RefreshToken { get; init; }
        public Guid UserId { get; init; }
        public TenantId TenantId { get; init; }
        public string? TenantSlug {get; init;}
    }
}