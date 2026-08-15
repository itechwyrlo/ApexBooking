using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.Core.Persistence.Seeders;
using ApexBooking.Core.Persistence.Services;
using ApexBooking.Core.Persistence.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApexBooking.Core.Domain.Services.Auth;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services.Notification;

namespace ApexBooking.Core.Persistence.Dependencies
{
    public static class PersistenceDependency
    {
        public static IServiceCollection AddPersistenceServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApexBookingDbContext>((sp, options) =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<IApplicationUserService, ApplicationUserService>();
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

            services.AddScoped<ISmsQuotaService, Services.SmsQuotaService>();
            services.AddScoped<IPlatformQueries, Services.PlatformQueries>();
            services.AddScoped<IOutboxStore, Services.OutboxStore>();
            services.AddScoped<IRefundRequestStore, Services.RefundRequestStore>();

            services.Configure<SuperAdminSeedOptions>(configuration.GetSection("SuperAdminSeed"));

            return services;
        }
    }

}