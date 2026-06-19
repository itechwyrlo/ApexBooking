using ApexBooking.Infrastructure.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace ApexBooking.WebApi.Extensions;

/// <summary>
/// Extension for configuring rate limiting policies
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddApplicationRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rateLimitingOptions = configuration.GetSection("RateLimiting").Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            // Global sliding window policy - applies to all endpoints
            options.AddSlidingWindowLimiter(
                policyName: "global",
                limiterOptions =>
                {
                    limiterOptions.Window = TimeSpan.FromSeconds(rateLimitingOptions.GlobalWindowSeconds);
                    limiterOptions.PermitLimit = rateLimitingOptions.GlobalPermitLimit;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.SegmentsPerWindow = 8;
                });

            // Auth endpoints - stricter limit per IP
            options.AddSlidingWindowLimiter(
                policyName: "auth",
                limiterOptions =>
                {
                    limiterOptions.Window = TimeSpan.FromMinutes(rateLimitingOptions.AuthWindowMinutes);
                    limiterOptions.PermitLimit = rateLimitingOptions.AuthPermitLimit;
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.SegmentsPerWindow = 8;
                });

            // Default strategy - use IP address for partitioning
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rateLimitingOptions.GlobalPermitLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.GlobalWindowSeconds),
                        SegmentsPerWindow = 8,
                    }));
        });

        return services;
    }

    /// <summary>
    /// Apply rate limiting middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseApplicationRateLimiting(
        this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }
}
