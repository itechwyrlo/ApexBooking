using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Authentication.RefreshToken
{
    /// <summary>
    /// Rotates the caller's refresh token and mints a fresh access token. The secret itself is
    /// read from the HttpOnly cookie, never from the request body — IsPlatformAdmin only selects
    /// which of the two independent cookies (tenant vs superadmin) to read/write, set by which
    /// controller route was hit.
    /// </summary>
    public record RefreshTokenCommand(bool IsPlatformAdmin) : ICommand<RefreshTokenResponse>;
}
