using ApexBooking.Core.Domain.Services.Tenant;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.WebApi.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService, ITenantResolver tenantResolver)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Check for your custom platform admin claim
            var isPlatformAdmin = context.User.FindFirst("platform_admin")?.Value == "true";

            if (isPlatformAdmin)
            {
                // Bypass the tenant_id requirement entirely for global system admins
                await _next(context);
                return;
            }

            // Standard tenant identification routing
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantGuid))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Tenant context missing.");
                return;
            }

            tenantService.SetCurrentTenant(new TenantId(tenantGuid));
        }
        else
        {
            // Anonymous wizard traffic: resolve the tenant from the matched route's {slug} segment
            // (e.g. api/public/{slug}/bookings/...). UseRouting() runs before this middleware, so
            // the route value is already populated. Tenant.Slug is unique-indexed, so this never
            // resolves more than one tenant.
            var slug = context.GetRouteValue("slug") as string;
            if (slug is not null)
            {
                var resolvedTenantId = await tenantResolver.ResolveBySlugAsync(slug, context.RequestAborted);
                if (resolvedTenantId is null)
                {
                   
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync("No business was found for this booking page.");
                    return;
                }

                tenantService.SetCurrentTenant(resolvedTenantId);
            }
        }

        await _next(context);
    }
}
