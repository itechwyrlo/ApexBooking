using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetMyProfile
{
    public sealed record GetMyProfileQuery() : IQuery<StaffDto>;
}
