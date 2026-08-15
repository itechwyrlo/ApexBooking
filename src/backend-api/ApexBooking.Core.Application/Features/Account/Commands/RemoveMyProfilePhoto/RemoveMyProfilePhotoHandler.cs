using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Account.Commands.RemoveMyProfilePhoto
{
    public class RemoveMyProfilePhotoHandler : ICommandHandler<RemoveMyProfilePhotoCommand>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IFileStorageService _fileStorage;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveMyProfilePhotoHandler(
            IApplicationUserService applicationUserService,
            IFileStorageService fileStorage,
            IUserContextService userContext,
            ITenantEntity tenantEntity,
            IUnitOfWork unitOfWork)
        {
            _applicationUserService = applicationUserService;
            _fileStorage = fileStorage;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(RemoveMyProfilePhotoCommand command, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();

            var existingProfile = await _applicationUserService.GetProfileAsync(userId, cancellationToken);
            var oldPhotoUrl = existingProfile?.PhotoUrl;

            await _applicationUserService.UpdatePhotoAsync(userId, null, cancellationToken);

            var tenantId = _tenantEntity.TenantId;
            if (tenantId is not null)
            {
                var tenant = await _unitOfWork.TenantRepository.GetAsync(
                    predicate: t => t.TenantId == tenantId,
                    includes: t => t.Members);

                var currentMember = tenant?.Members.FirstOrDefault(m => m.UserId == userId);
                if (currentMember is not null)
                {
                    currentMember.UpdatePhoto(null);
                    _unitOfWork.TenantRepository.Update(tenant!);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
            }

            if (!string.IsNullOrEmpty(oldPhotoUrl))
                await _fileStorage.DeleteAsync(oldPhotoUrl, cancellationToken);
        }
    }
}
