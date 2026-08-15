using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetAllServices
{
    public static class ServiceCatalogMappingProfile
    {
        public static void AddMappingConfigs(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ServiceId, Guid>().ConvertUsing(id => id.Value);

            cfg.CreateMap<Service, ServiceCatalogSummary>();
        }
    }
}