using ApexBooking.Core.Domain.Services.Tenant;
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
    
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantGuid))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Tenant context missing.");
                return;
            }

            tenantService.SetCurrentTenant(new TenantId(tenantGuid));

        }

        await _next(context);
    }
}
