using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;

namespace ApexBooking.Core.Application.Features.Authentication.FindWorkspace
{
    public class FindWorkspaceHandler : IQueryHandler<FindWorkspaceQuery, FindWorkspaceResult>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IUnitOfWork _unitOfWork;

        public FindWorkspaceHandler(IApplicationUserService applicationUserService, IUnitOfWork unitOfWork)
        {
            _applicationUserService = applicationUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task<FindWorkspaceResult> Handle(FindWorkspaceQuery query, CancellationToken cancellationToken)
        {
            var userId = await _applicationUserService.FindUserIdByEmailAsync(query.Email, cancellationToken);
            if (userId is null)
                return new FindWorkspaceResult(null);

            // Returns null for: no active tenant, or an inactive membership — same "just not found"
            // outcome as an unknown email, from the caller's point of view.
            var tenant = await _unitOfWork.TenantRepository.GetByUserIdAsync(userId.Value, cancellationToken);

            return new FindWorkspaceResult(tenant?.Slug);
        }
    }
}
