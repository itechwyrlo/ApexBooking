using ApexBooking.Core.Application.Dependency;
using ApexBooking.Core.Persistence.Dependencies;
using ApexBooking.Core.Persistence.Seeders;
using ApexBooking.Infrastructure.Dependency;
using ApexBooking.Infrastructure.Configuration;
using ApexBooking.WebApi.Dependency;
using ApexBooking.WebApi.Extensions;
using ApexBooking.WebApi.Middleware;
using FluentValidation;
using Microsoft.Extensions.Options;
using ApexBooking.WebApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
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

// Configure logging
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

// Add configuration validation (must be before any service that uses configuration)
builder.Services.AddConfigurationValidation(builder.Configuration, builder.Environment);

// Controllers
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// API Configuration (Auth, Tenant Service)
builder.Services.AddApiConfiguration(builder.Configuration);

// Enhanced CORS with configurable policy
builder.Services.AddApplicationCors(builder.Configuration, builder.Environment);

// Rate Limiting
builder.Services.AddApplicationRateLimiting(builder.Configuration);

// Configure Options from configuration
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));

// Register all layer services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureService(builder.Configuration);

// OpenAPI/Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

await SuperAdminSeeder.SeedAsync(app.Services);

// Get security options from DI for use in middleware
var securityOptions = app.Services.GetRequiredService<IOptions<SecurityOptions>>().Value;

// --- 2. MIDDLEWARE PIPELINE (ORDER MATTERS) ---

// Security headers FIRST (before any other middleware)
app.UseSecurityHeaders();

// Enforce HTTPS if configured
if (securityOptions.RequireHttps)
{
    app.UseApplicationSecurityPolicy(securityOptions);
}

// Exception handling
app.UseExceptionHandler();
// app.UseExceptionHandler("/error");

// Serves static files (React, CSS, JS) from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Routing
app.UseRouting();

// Rate Limiting (before CORS)
app.UseApplicationRateLimiting();

// CORS (before Authentication)
app.UseApplicationCors();

// Authentication & Authorization
app.UseAuthentication();

// Multi-tenant context extraction
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

// OpenAPI only in Development with minimal exposure
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Optionally add Swagger UI (commented for security in production-like environments)
    // app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "API v1"));
}

// --- 3. ROUTING ---

// Map API Controllers (take priority over fallback)
app.MapControllers();

// Fallback to React SPA (must be last)
app.MapFallbackToController("Index", "Fallback");

app.Run();