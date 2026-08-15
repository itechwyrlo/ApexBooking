using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfile
{
    public class UpdateMyProfileHandler : ICommandHandler<UpdateMyProfileCommand>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateMyProfileHandler(
            IApplicationUserService applicationUserService,
            IUserContextService userContext,
            ITenantEntity tenantEntity,
            IUnitOfWork unitOfWork)
        {
            _applicationUserService = applicationUserService;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateMyProfileCommand command, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();

            await _applicationUserService.UpdateProfileAsync(
                userId, command.FirstName, command.LastName, command.PhoneNumber, cancellationToken);

            // Keep the tenant-facing copy (team lists, idle-staff lists, booking pickers) in
            // sync. SuperAdmin has no tenant context and no TenantMember row, so this is a no-op
            // for them; a deactivated/removed member is also a silent no-op — the ApplicationUser
            // update above already succeeded and must not be rolled back over a stale membership.
            var tenantId = _tenantEntity.TenantId;
            if (tenantId is null)
                return;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members);

            var currentMember = tenant?.Members.FirstOrDefault(m => m.UserId == userId);
            if (currentMember is null)
                return;

            currentMember.UpdateProfile(command.FirstName, command.LastName, command.PhoneNumber ?? string.Empty, currentMember.CustomJobTitle);

            _unitOfWork.TenantRepository.Update(tenant!);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
