using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record RefreshTokenResponseDto
    {
        public string AccessToken { get; init; }
        public Guid UserId { get; init; }
        public TenantId TenantId { get; init; }
        
    
    }
}