using ApexBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ApexBooking.WebApi.Extensions;

/// <summary>
/// Extension for configuring CORS with configurable policies from appsettings
/// </summary>
public static class CorsConfigurationExtensions
{
    public static IServiceCollection AddApplicationCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var corsOptions = configuration.GetSection("Cors").Get<CorsOptions>()
            ?? new CorsOptions();

        services.AddCors(options =>
        {
            // Get allowed origins based on environment
            var allowedOrigins = corsOptions.AllowedOrigins;
            
            if (environment.IsDevelopment() && string.IsNullOrEmpty(allowedOrigins))
            {
                // Default development origins
                allowedOrigins = "http://*.localhost:5096";
            }

            var policy = "ApplicationCorsPolicy";
            options.AddPolicy(policy, builder =>
            {
                // Parse allowed origins (supports wildcards)
                if (!string.IsNullOrEmpty(allowedOrigins))
                {
                    var origins = allowedOrigins.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    
                    // Check if wildcard patterns are used
                    if (origins.Any(o => o.Contains("*")))
                    {
                        // Use SetIsOriginAllowed for pattern matching
                        builder.SetIsOriginAllowed(origin =>
                        {
                            return origins.Any(pattern => MatchesPattern(origin, pattern));
                        });
                    }
                    else
                    {
                        // Use WithOrigins for exact matches
                        builder.WithOrigins(origins);
                    }
                }

                // Configure headers and methods
                if (corsOptions.AllowedHeaders.Any())
                {
                    builder.WithHeaders(corsOptions.AllowedHeaders.ToArray());
                }
                else
                {
                    builder.AllowAnyHeader();
                }

                if (corsOptions.AllowedMethods.Any())
                {
                    builder.WithMethods(corsOptions.AllowedMethods.ToArray());
                }
                else
                {
                    builder.AllowAnyMethod();
                }

                if (corsOptions.AllowCredentials)
                {
                    builder.AllowCredentials();
                }

                builder.WithExposedHeaders("X-Total-Count"); // For pagination
                builder.SetPreflightMaxAge(TimeSpan.FromSeconds(corsOptions.MaxAgeSeconds));
            });
        });

        return services;
    }

    /// <summary>
    /// Pattern matching for CORS origins with wildcard support
    /// </summary>
    private static bool MatchesPattern(string origin, string pattern)
    {
        if (pattern == "*")
            return true;

        // Handle *.localhost:5096 pattern
        if (pattern.StartsWith("*."))
        {
            var suffix = pattern.Substring(1); // Remove * but keep .
            return origin.EndsWith(suffix);
        }

        // Exact match
        return origin == pattern;
    }

    public static IApplicationBuilder UseApplicationCors(
        this IApplicationBuilder app)
    {
        app.UseCors("ApplicationCorsPolicy");
        return app;
    }
}
