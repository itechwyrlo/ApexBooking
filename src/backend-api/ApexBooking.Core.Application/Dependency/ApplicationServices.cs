using System.Reflection;
using ApexBooking.Core.Application.Common.Behaviors;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Application.Common.Outbox;
using ApexBooking.Core.Application.Common.ReferenceData.Psgc;
using ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile;
using ApexBooking.Core.Application.Features.Services.Queries.GetAllServices;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllTenantBookings;
using ApexBooking.Core.Application.Features.TenantRequest.Queries.GetAllRequest;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Models;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApexBooking.Core.Application.Dependency
{
    public static class ApplicationServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Register MediatR with assembly scanning
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

            // Register AutoMapper — scans this assembly for Profile classes (there are none; every
            // mapping lives in a plain static *MappingProfile.AddMappingConfigs helper instead) and
            // applies the explicit configs below. Must be a SINGLE AddAutoMapper call: calling it
            // twice is a known DI footgun where the second registration's config is silently
            // dropped, leaving IMapper with no type maps at all.
            services.AddAutoMapper(cfg =>
            {
                TenantRequestMappingProfile.AddMappingConfigs(cfg);
                TeamMemberMappingProfile.AddMappingConfigs(cfg);
                ServiceCatalogMappingProfile.AddMappingConfigs(cfg);
                MyProfileMappingProfile.AddMappingConfigs(cfg);
            }, assembly);

            // Register all FluentValidation validators from this assembly
            services.AddValidatorsFromAssembly(assembly);

            // Register validation behavior in MediatR pipeline
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            // Domain-event dispatch (wraps IDomainEvent -> MediatR notification)
            services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

            // Outbox relay: replays stored IReliableDomainEvent payloads through the same MediatR
            // handlers. Lets the Hangfire relay job (Infrastructure) trigger this without
            // Infrastructure ever referencing Core.Application directly — see IOutboxRelayService.
            services.AddScoped<IOutboxRelayService, OutboxRelayService>();

            // Post-commit SignalR push for Notification rows — see IRealtimeNotificationDispatcher.
            services.AddScoped<Common.Notifications.IRealtimeNotificationDispatcher, Common.Notifications.RealtimeNotificationDispatcher>();

            // Static PSGC reference dataset, loaded once from embedded resources.
            services.AddSingleton<IPsgcReferenceService, PsgcReferenceService>();

            return services;
        }

        public static IServiceCollection AddWebApiValidators(this IServiceCollection services)
        {
            try
            {
                var webApiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "ApexBooking.WebApi");

                if (webApiAssembly != null)
                {
                    services.AddValidatorsFromAssembly(webApiAssembly);
                }
            }
            catch
            {
                // If WebApi assembly is not loaded yet, it will be registered when validators are used
            }

            return services;
        }
    }
}