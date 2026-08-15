using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Domain.Services;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile
{
    public static class MyProfileMappingProfile
    {
        public static void AddMappingConfigs(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ApplicationUserProfile, MyProfileDto>();
        }
    }
}
