using ApexBooking.Core.Application.Dtos.Request;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.TenantRequest.Queries.GetAllRequest
{
    public static class TenantRequestMappingProfile
    {
        public static void AddMappingConfigs(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<TenantRegistrationRequestId, Guid>().ConvertUsing(id => id.Value);

            cfg.CreateMap<TenantRegistrationRequest, PendingTenantRequestSummary>()
                .ForMember(dest => dest.BusinessType, opt => opt.MapFrom(src => src.BusinessType.ToString()))
                .ForMember(dest => dest.RequestedPlan, opt => opt.MapFrom(src => src.RequestedPlan.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}