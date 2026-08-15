using System.Security.Cryptography;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.Core.Persistence.Identity;
using ApexBooking.Core.Persistence.Settings;
using ApexBooking.Core.Persistence.TokenProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace ApexBooking.WebApi.Extensions
{
    public static class AuthenticationConfiguration
    {
        public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration config)
        {
            // Microsoft Identity
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(opts =>
                {
                    opts.SignIn.RequireConfirmedEmail = true;
                    opts.Password.RequiredLength = 8;
                    opts.Password.RequireDigit = true;
                    opts.Password.RequireUppercase = true;
                    opts.Lockout.MaxFailedAccessAttempts = 5;
                    opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<ApexBookingDbContext>()
                .AddTokenProvider<EmailVerificationTokenProvider>("EmailVerification")
                .AddTokenProvider<PasswordResetTokenProvider>("PasswordReset")
                .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(opts =>
            {
                // Governs the invitation/setup-password link (owner + staff, admin included since
                // admin is a staff role) — must match the "72 hours" promised in the invite email copy.
                opts.TokenLifespan = TimeSpan.FromHours(72);
            });

            // JWT Authentication (RS256)
            var jwtSection = config.GetSection("Jwt");
            services.Configure<JwtSettings>(jwtSection);

            var rsa = RSA.Create();
            rsa.ImportFromPem(jwtSection["PublicKeyPem"]
                ?? throw new InvalidOperationException("JWT PublicKeyPem not configured."));

            services
                .AddAuthentication(opts =>
                {
                    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(opts =>
                {
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSection["Issuer"],
                        ValidAudience = jwtSection["Audience"],
                        IssuerSigningKey = new RsaSecurityKey(rsa),
                        ClockSkew = TimeSpan.Zero
                    };

                    // SignalR's browser transports (WebSockets/SSE) can't set a custom Authorization
                    // header, so the client sends the token as an "access_token" query string param
                    // instead (see notificationHubConnection.ts's accessTokenFactory). Standard
                    // ASP.NET Core pattern for JWT-authenticated hubs — without this, NotificationHub's
                    // [Authorize] would reject every SignalR connection.
                    opts.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(accessToken) &&
                                context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // Authorization policies
            services.AddAuthorization(opts =>
            {
                opts.AddPolicy("OwnerOnly", p => p.RequireRole("owner"));
                opts.AddPolicy("Staff", p => p.RequireRole("staff"));
                opts.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
                opts.AddPolicy("SuperAdminOnly", p => p.RequireClaim("platform_admin", "true"));

                opts.AddPolicy("ManagementOnly", policy => policy.RequireAssertion(context =>
                context.User.IsInRole("Owner") ||
                context.User.IsInRole("Admin") ||
                context.User.HasClaim("platform_admin", "true")));
            });

            return services;
        }
    }

}