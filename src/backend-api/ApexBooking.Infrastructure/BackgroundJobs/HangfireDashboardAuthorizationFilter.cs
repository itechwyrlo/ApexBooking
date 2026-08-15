using Hangfire.Dashboard;

namespace ApexBooking.Infrastructure.BackgroundJobs;

/// <summary>
/// Restricts /hangfire to platform admins — the same "platform_admin" claim the existing
/// SuperAdminOnly authorization policy checks (see AuthenticationExtensions.cs), just expressed
/// the way Hangfire's dashboard requires: it's raw middleware, not an MVC endpoint, so the
/// [Authorize] attribute doesn't apply to it directly.
///
/// Must be mounted after app.UseAuthentication() in Program.cs's middleware pipeline, or
/// httpContext.User won't be populated yet when Authorize() runs.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.HasClaim("platform_admin", "true");
    }
}
