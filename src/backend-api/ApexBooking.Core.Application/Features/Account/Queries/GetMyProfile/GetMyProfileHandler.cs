using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Exceptions;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.Account.Queries.GetMyProfile
{
    public class GetMyProfileHandler : IQueryHandler<GetMyProfileQuery, MyProfileDto>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IUserContextService _userContext;
        private readonly IMapper _mapper;

        public GetMyProfileHandler(IApplicationUserService applicationUserService, IUserContextService userContext, IMapper mapper)
        {
            _applicationUserService = applicationUserService;
            _userContext = userContext;
            _mapper = mapper;
        }

        public async Task<MyProfileDto> Handle(GetMyProfileQuery query, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();
            var profile = await _applicationUserService.GetProfileAsync(userId, cancellationToken)
                ?? throw new BusinessRuleBrokenException("Failed to load profile. User not found.");

            return _mapper.Map<MyProfileDto>(profile);
        }
    }
}
