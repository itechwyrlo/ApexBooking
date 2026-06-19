using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.SharedKernel.Services;
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