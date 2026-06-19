using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record TenantRegistrationResponseDto(string TenantSlug);
}
