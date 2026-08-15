using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile
{
    public record GetMyProfileQuery : IQuery<MyProfileDto>;
}
