using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfilePhoto
{
    public class UpdateMyProfilePhotoHandler : ICommandHandler<UpdateMyProfilePhotoCommand, string>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IFileStorageService _fileStorage;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateMyProfilePhotoHandler(
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

        public async Task<string> Handle(UpdateMyProfilePhotoCommand command, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();

            var existingProfile = await _applicationUserService.GetProfileAsync(userId, cancellationToken);
            var oldPhotoUrl = existingProfile?.PhotoUrl;

            var fileName = $"{userId}/{Guid.NewGuid()}{command.FileExtension}";
            var newPhotoUrl = await _fileStorage.SaveAsync(command.Content, fileName, command.ContentType, cancellationToken);

            await _applicationUserService.UpdatePhotoAsync(userId, newPhotoUrl, cancellationToken);

            var tenantId = _tenantEntity.TenantId;
            if (tenantId is not null)
            {
                var tenant = await _unitOfWork.TenantRepository.GetAsync(
                    predicate: t => t.TenantId == tenantId,
                    includes: t => t.Members);

                var currentMember = tenant?.Members.FirstOrDefault(m => m.UserId == userId);
                if (currentMember is not null)
                {
                    currentMember.UpdatePhoto(newPhotoUrl);
                    _unitOfWork.TenantRepository.Update(tenant!);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }
            }

            if (!string.IsNullOrEmpty(oldPhotoUrl))
                await _fileStorage.DeleteAsync(oldPhotoUrl, cancellationToken);

            return newPhotoUrl;
        }
    }
}
