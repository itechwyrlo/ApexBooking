using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.Infrastructure.Configuration;
using ApexBooking.Infrastructure.ExternalServices.TokenProviders;
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
            services.AddIdentity<User, IdentityRole<Guid>>(opts =>
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
                opts.TokenLifespan = TimeSpan.FromHours(24); // 24 hours for email verification
            });

            // JWT Authentication (RS256)
            var jwtSection = config.GetSection("Jwt");
            services.Configure<JwtOptions>(jwtSection);

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

                    // JTI blacklist check
                    opts.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = ctx =>
                        {
                            var tokenSvc = ctx.HttpContext.RequestServices.GetRequiredService<ITokenService>();
                            var jti = ctx.Principal?.FindFirst("jti")?.Value;
                            if (jti is not null && tokenSvc.IsJtiBlacklisted(jti))
                            {
                                ctx.Fail("Token has been revoked.");
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // Authorization policies
            services.AddAuthorization(opts =>
            {
                opts.AddPolicy("CustomerOnly", p => p.RequireRole("customer"));
                opts.AddPolicy("StaffAndAbove", p => p.RequireRole("staff", "manager", "tenantadmin"));
                opts.AddPolicy("ManagerAndAbove", p => p.RequireRole("manager", "tenantadmin"));
                opts.AddPolicy("AdminOnly", p => p.RequireRole("tenantadmin"));
                opts.AddPolicy("SuperAdminOnly", p => p.RequireRole("superadmin"));
                opts.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());
            });

            return services;
        }
    }

}