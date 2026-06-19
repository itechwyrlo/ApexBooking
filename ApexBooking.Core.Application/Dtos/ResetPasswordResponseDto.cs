using System.Collections.Generic;
using ApexBooking.SharedKernel.Models;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record ResetPasswordResponseDto
    {
        public string? AccessToken { get; init; }
        public string? RefreshToken { get; init; }
        public Guid UserId { get; init; }
        public TenantId? TenantId { get; init; }
        
    }
}
