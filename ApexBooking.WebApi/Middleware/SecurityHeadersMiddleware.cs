using ApexBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ApexBooking.WebApi.Middleware;

/// <summary>
/// Middleware to add security headers to all responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _securityOptions;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityOptions> securityOptions)
    {
        _next = next;
        _securityOptions = securityOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent browsers from MIME-sniffing the response
        if (_securityOptions.EnableContentTypeOptions)
        {
            headers.Append("X-Content-Type-Options", "nosniff");
        }

        // Prevent clickjacking attacks
        if (_securityOptions.EnableFrameOptions)
        {
            headers.Append("X-Frame-Options", "DENY");
        }

        // Enable browser XSS protection
        headers.Append("X-XSS-Protection", "1; mode=block");

        // Add Content-Security-Policy if enabled
        if (_securityOptions.EnableCsp)
        {
            headers.Append("Content-Security-Policy", _securityOptions.CspPolicy);
        }

        // Referrer-Policy: Don't send referrer to less secure origins
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions-Policy: Restrict access to sensitive features
        headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

        await _next(context);
    }
}

/// <summary>
/// Extension to add security headers middleware
/// </summary>
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }

    /// <summary>
    /// Configure HTTPS redirection and HSTS
    /// </summary>
    public static IApplicationBuilder UseApplicationSecurityPolicy(
        this IApplicationBuilder app,
        SecurityOptions securityOptions)
    {
        if (securityOptions.RequireHttps)
        {
            // Redirect HTTP to HTTPS
            app.UseHttpsRedirection();

            // Add HSTS header for HTTPS connections
            if (securityOptions.EnableHsts)
            {
                app.UseHsts();
            }
        }

        return app;
    }
}
