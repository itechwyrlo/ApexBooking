using ApexBooking.Core.Domain.Services.Tenant;
using ApexBooking.Infrastructure.ExternalServices.Tenant;
using ApexBooking.WebApi.Extensions;

namespace ApexBooking.WebApi.Dependency
{
    public static class ApiDependencies
    {
        public static IServiceCollection AddApiConfiguration(this IServiceCollection service, IConfiguration config)
        {
            service.AddAuthenticationConfiguration(config);
            service.AddScoped<ITenantService, TenantService>();
            return service;
        }
    }
}