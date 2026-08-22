using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.EmailNotification;
using ApexBooking.Infrastructure.BackgroundJobs;
using ApexBooking.Infrastructure.ExternalServices.Storage;
using ApexBooking.Infrastructure.Configuration;
using ApexBooking.Infrastructure.ExternalServices;
using ApexBooking.Infrastructure.ExternalServices.AuthNotificationService;
using ApexBooking.Infrastructure.ExternalServices.Brevo;
using ApexBooking.Infrastructure.ExternalServices.Context;
using ApexBooking.Infrastructure.ExternalServices.Cookie;
using ApexBooking.Infrastructure.ExternalServices.DataProtection;
using ApexBooking.Infrastructure.ExternalServices.Realtime;
using ApexBooking.Infrastructure.Hubs;
using ApexBooking.SharedKernel.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApexBooking.Core.Domain.Policies;
using ApexBooking.Infrastructure.Policies;
using ApexBooking.Infrastructure.ExternalServices.BookingNotificationService;
using ApexBooking.Infrastructure.ExternalServices.PayMongo;
using ApexBooking.Core.Domain.Services.Paymongo;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.Infrastructure.Ticketing;
using ApexBooking.Core.Domain.Services.Tenant;
using ApexBooking.Infrastructure.ExternalServices.Tenant;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using ApexBooking.Core.Domain.Services.Notification.Tenancy;
using ApexBooking.Core.Domain.Services.BookingEngine;
using ApexBooking.Infrastructure.BookingEngine;
using ApexBooking.Infrastructure.ExternalServices.TenantLifecycleNotificationService;

namespace ApexBooking.Infrastructure.Dependency
{
    public static class InfrastructureDependencies
    {
        public static IServiceCollection AddInfrastructureService(this IServiceCollection service, IConfiguration config)
        {
            service.AddHttpContextAccessor();

            // Persists the Data Protection key ring into SQL Server via ApexBookingDbContext,
            // instead of the default local-disk key store — required for correctness once this app
            // runs on more than one instance, and for TenantPaymentCredential encryption (see
            // ISecretProtector) to survive a redeploy. AddIdentity() (AuthenticationExtensions.cs)
            // already implicitly calls AddDataProtection() for its own token providers; this call
            // reconfigures that same registration's key-storage mechanism — AddDataProtection() is
            // safe to call more than once.
            service.AddDataProtection()
                .PersistKeysToDbContext<ApexBooking.Core.Persistence.Data.ApexBookingDbContext>()
                .SetApplicationName("ApexBooking");

            service.AddScoped<IUserContextService, UserContextService>();
            service.AddScoped<ITenantEntity, HttpContextTenantIdProvider>();
            service.AddScoped<ITenantResolver, TenantResolver>();


            // Notification delivery + plan policy (08).
            service.AddScoped<ISmsService, ExternalServices.Notification.SmsService>();
            service.AddSingleton<IPlanPolicy, ExternalServices.Plan.PlanPolicy>();

            service.AddHttpClient<IPayMongoService, PayMongoService>();
            service.AddSingleton<IPayMongoWebhookSignatureVerifier, PayMongoWebhookSignatureVerifier>();
            service.AddScoped<INotificationService, BrevoSmtpService>();
            // Named/timed client for BrevoSmtpService — see its doc comment for why the untyped
            // default (100s timeout) is a problem for a background worker.
            service.AddHttpClient("Brevo", client => client.Timeout = TimeSpan.FromSeconds(15));
            service.AddScoped<IAuthNotificationService, AuthNotificationService>();
            service.AddTransient<IBookingNotificationService, BookingNotificationService>();
            service.AddTransient<ITenantLifecycleNotificationService, TenantLifecycleNotificationService>();
            service.AddScoped<ICookieService, CookieService>();
            service.AddScoped<IFileStorageService, LocalDiskFileStorageService>();
            service.AddScoped<IAppUrlService, AppUrlService>();
            service.AddSingleton<ITicketTokenService, HmacTicketTokenService>();
            service.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
            service.AddSingleton<IQrCodeGenerator, QrCoderQrCodeGenerator>();
            service.AddSingleton<ICancellationTokenService, HmacCancellationTokenService>();

            service.Configure<AppSettings>(config.GetSection("AppSettings"));
            service.Configure<ApplicationUrlsSettings>(config.GetSection("ApplicationUrls"));
            service.Configure<EmailSettings>(config.GetSection("EmailSettings"));

            service.AddSignalR();
            service.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();
            service.AddScoped<IRealtimeNotificationService, SignalRNotificationService>();

            service.AddHttpClient();

            // Hangfire: Outbox Relay (design spec: 2026-08-07-hangfire-outbox-core-design.md) +
            // Trial Expiry sweep (TrialExpiryJob registered inside AddHangfireBackgroundJobs).
            service.AddHangfireBackgroundJobs(config);
            service.AddSingleton<ISlugValidationPolicy, SlugValidationPolicy>();
            service.AddSingleton<ISlotGenerator, SlotGenerator>();

            return service;
        }
    }
}