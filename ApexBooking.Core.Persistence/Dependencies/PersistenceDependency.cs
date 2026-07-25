using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.Core.Persistence.Interceptors;
using ApexBooking.Core.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApexBooking.Core.Persistence.Dependencies
{
    public static class PersistenceDependency
    {
        public static IServiceCollection AddPersistenceServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<DispatchDomainEventsInterceptor>();

            services.AddDbContext<ApexBookingDbContext>((sp, options) =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                       .AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>()));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.Configure<SuperAdminSeedOptions>(configuration.GetSection("SuperAdminSeed"));

            return services;
        }
    }

}