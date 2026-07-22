using ApexBooking.Core.Application.Dependency;
using ApexBooking.Core.Persistence.Dependencies;
using ApexBooking.Core.Persistence.Seeders;
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
app.UseStaticFiles();

app.UseRouting();

app.UseApplicationRateLimiting();
app.UseApplicationCors();

app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- 3. ROUTING ---

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapFallbackToController("Index", "Fallback");

app.Run();
