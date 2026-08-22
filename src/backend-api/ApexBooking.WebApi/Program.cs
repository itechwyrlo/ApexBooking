using ApexBooking.Core.Application.Dependency;
using ApexBooking.Core.Persistence.Dependencies;
using ApexBooking.Core.Persistence.Seeders;
using ApexBooking.Infrastructure.BackgroundJobs;
using ApexBooking.Infrastructure.Dependency;
using ApexBooking.Infrastructure.Configuration;
using ApexBooking.Infrastructure.Hubs;
using ApexBooking.WebApi.Dependency;
using ApexBooking.WebApi.Extensions;
using ApexBooking.WebApi.Middleware;
using Microsoft.Extensions.Options;
using ApexBooking.WebApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.UseUtcTimestamp = true;
    });
    builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
}

// --- 1. SERVICES REGISTRATION ---

builder.Services.AddConfigurationValidation(builder.Configuration, builder.Environment);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApiConfiguration(builder.Configuration);
builder.Services.AddApplicationCors(builder.Configuration, builder.Environment);
builder.Services.AddApplicationRateLimiting(builder.Configuration);

builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureService(builder.Configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

await SuperAdminSeeder.SeedAsync(app.Services);

var securityOptions = app.Services.GetRequiredService<IOptions<SecurityOptions>>().Value;

// --- 2. MIDDLEWARE PIPELINE (ORDER MATTERS) ---

if (securityOptions.RequireHttps)
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    await next();
});

app.UseExceptionHandler();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // The Vite build content-hashes everything under /assets (e.g. index-BTqUe2Ao.js) — a new
        // deploy produces new filenames, so these are safe to cache forever. Everything else
        // (index.html, sw.js, registerSW.js, manifest.webmanifest) is the opposite: it's what
        // POINTS AT those hashed filenames, and must always be revalidated — otherwise a stale
        // cached index.html after a deploy can reference a bundle filename the new build already
        // deleted, breaking the app until the user hard-refreshes.
        var isImmutableAsset = ctx.Context.Request.Path.StartsWithSegments("/assets");

        ctx.Context.Response.Headers["Cache-Control"] = isImmutableAsset
            ? "public,max-age=31536000,immutable"
            : "no-cache";
    }
});

app.UseRouting();

// CORS must run before anything that can short-circuit the pipeline (rate limiting included) —
// otherwise a rejected response never gets Access-Control-Allow-* headers attached, and the
// browser reports a generic "CORS error" that has nothing to do with the actual CORS policy.
app.UseApplicationCors();
app.UseApplicationRateLimiting();

app.UseAuthentication();

// Must come after UseAuthentication() — the dashboard's authorization filter reads
// httpContext.User, which isn't populated until authentication middleware has run.
app.UseHangfireBackgroundJobs();

app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- 3. ROUTING ---

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// SPA fallback — must be mapped last. ASP.NET Core only falls through to this for GET
// requests that matched neither a controller/hub route above nor an existing static file
// (via UseStaticFiles() earlier in the pipeline), so /api/*, /hubs/*, and real wwwroot
// assets are all resolved before this ever runs. This is what lets client-side routes like
// /:tenantSlug/login or /:tenantSlug/dashboard survive a direct navigation or a refresh —
// previously disabled (see prior commented-out MapFallbackToController call, which pointed
// at a "Fallback" controller that was never implemented) and enabled as part of the
// client-app / wwwroot consolidation so the SPA behaves correctly once deployed.
app.MapFallbackToFile("index.html");

app.Run();
