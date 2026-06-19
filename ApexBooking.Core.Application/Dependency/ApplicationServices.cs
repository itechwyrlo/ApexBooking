using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.Behaviors;
using ApexBooking.Core.Domain.Services.Slot;
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

            // Register all FluentValidation validators from this assembly
            services.AddValidatorsFromAssembly(assembly);

            // Register validation behavior in MediatR pipeline
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Register domain services
            services.AddSingleton<SlotAvailabilityService>();

            return services;
        }

        /// <summary>
        /// Register validators from the WebApi assembly (where DTOs are located)
        /// Call this from Program.cs after AddApplicationServices
        /// </summary>
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