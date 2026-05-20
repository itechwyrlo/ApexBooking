using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Interfaces;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.EmailNotification;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.Infrastructure.BackgroundJobs;
using ApexBooking.Infrastructure.Configuration;
using ApexBooking.Infrastructure.ExternalServices;
using ApexBooking.Infrastructure.ExternalServices.AuthNotificationService;
using ApexBooking.Infrastructure.ExternalServices.BookingNotificationService;
using ApexBooking.Infrastructure.ExternalServices.Brevo;
using ApexBooking.Infrastructure.ExternalServices.Context;
using ApexBooking.Infrastructure.ExternalServices.Cookie;
using ApexBooking.Infrastructure.ExternalServices.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApexBooking.Infrastructure.Dependency
{
    public static class InfrastructureDependencies
    {
        public static IServiceCollection AddInfrastructureService(this IServiceCollection service, IConfiguration config)
        {
            service.AddHttpContextAccessor();
            service.AddMemoryCache();
            service.AddScoped<IUserContextService, UserContextService>();
            service.AddScoped<IBookingNotificationService, BookingNotificationService>();
            service.AddScoped<ITokenService, JwtTokenService>();
            service.AddScoped<INotificationService, BrevoSmtpService>();
            service.AddScoped<IAuthNotificationService, AuthNotificationService>();
            service.AddScoped<ICookieService, CookieService>();
            service.AddScoped<IAppUrlService, AppUrlService>();

            service.Configure<AppSettings>(config.GetSection("AppSettings"));
            service.Configure<EmailSettings>(config.GetSection("EmailSettings"));
            service.Configure<JwtOptions>(config.GetSection("Jwt"));
            service.AddScoped<IPayPalWebhookValidator, PayPalWebhookValidator>();

            service.AddHttpClient();

            // Background jobs
            service.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            service.AddScoped<TrialExpiryJob>();
            service.AddHostedService<BackgroundWorker>();
            service.AddHostedService<TrialExpiryWorker>();

            return service;
        }
    }
}