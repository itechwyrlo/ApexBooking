using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Authentication.Login
{
    // Slug travels as a route segment (/api/{slug}/auth/login), not the request body — TenantMiddleware
    // already resolves it into the ambient ITenantEntity for any unauthenticated request whose route
    // has a {slug} segment (same mechanism the public booking wizard uses), so the handler reads it
    // from there rather than re-resolving it itself.
    public record TenantLoginCommand(string Email, string Password) : ICommand<LoginResponse>;
}
