using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllTenantBookings
{
    public static class TeamMemberMappingProfile
    {
        public static void AddMappingConfigs(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<TenantMemberId, Guid>().ConvertUsing(id => id.Value);

            cfg.CreateMap<TenantMember, TeamMemberSummary>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

            cfg.CreateMap<TenantMember, BookableStaffSummary>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()));

        }
    }
}