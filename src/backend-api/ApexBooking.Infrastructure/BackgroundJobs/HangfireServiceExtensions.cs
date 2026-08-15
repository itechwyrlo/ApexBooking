using ApexBooking.Core.Domain.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApexBooking.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire wiring for the Outbox Relay (design spec: 2026-08-07-hangfire-outbox-core-design.md)
/// and the Trial Expiry sweep. Follows the same "one Add*/one Use*" shape as the rest of this
/// codebase's composition root (AddApplicationServices, AddPersistenceServices,
/// AddInfrastructureService) rather than being wired loose in Program.cs.
/// </summary>
public static class HangfireServiceExtensions
{
    private const string OutboxRelaySweepJobId = "outbox-relay-sweep";
    private const string TrialExpirySweepJobId = "trial-expiry-sweep";

    public static IServiceCollection AddHangfireBackgroundJobs(this IServiceCollection services, IConfiguration config)
    {
        services.AddHangfire(cfg => cfg
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                config.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    // Same database as ApexBookingDbContext, own schema — one connection string to
                    // operate, no second database to provision.
                    SchemaName = "hangfire",
                    PrepareSchemaIfNecessary = true,
                })
            .UseFilter(new AutomaticRetryAttribute { Attempts = 3 }));

        services.AddHangfireServer();

        services.AddScoped<IOutboxTrigger, HangfireOutboxTrigger>();
        services.AddScoped<OutboxRelayJob>();
        services.AddScoped<TrialExpiryJob>();

        return services;
    }

    public static IApplicationBuilder UseHangfireBackgroundJobs(this IApplicationBuilder app)
    {
        // Must run after app.UseAuthentication() — HangfireDashboardAuthorizationFilter reads
        // httpContext.User, which isn't populated until authentication middleware has run.
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
        });

        RecurringJob.AddOrUpdate<OutboxRelayJob>(
            OutboxRelaySweepJobId,
            job => job.ReplayPending(JobCancellationToken.Null),
            "*/1 * * * *", // standard cron's finest granularity is 1 minute — see OutboxRelayJob's
                           // doc comment for why the immediate trigger, not this sweep, is what
                           // actually delivers low latency.
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        RecurringJob.AddOrUpdate<TrialExpiryJob>(
            TrialExpirySweepJobId,
            job => job.Run(JobCancellationToken.Null),
            Cron.Hourly(),
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        return app;
    }
}
