using System.Security.Claims;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.WebApi.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Super admins are platform-level operators with no tenant affiliation.
            // They carry role=superadmin but no tenant_id claim.
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                       ?? context.User.FindFirst("role")?.Value;
            if (role == "superadmin")
            {
                await _next(context);
                return;
            }

            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantGuid))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Tenant context missing.");
                return;
            }

            tenantService.TenantId = new TenantId(tenantGuid);
        }

        await _next(context);
    }
}
