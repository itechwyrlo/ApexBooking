using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;

namespace ApexBooking.Core.Application.Features.Account.Commands.ChangeMyPassword
{
    public class ChangeMyPasswordHandler : ICommandHandler<ChangeMyPasswordCommand>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IUserContextService _userContext;

        public ChangeMyPasswordHandler(IApplicationUserService applicationUserService, IUserContextService userContext)
        {
            _applicationUserService = applicationUserService;
            _userContext = userContext;
        }

        public async Task Handle(ChangeMyPasswordCommand command, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();

            await _applicationUserService.ChangePasswordAsync(
                userId, command.CurrentPassword, command.NewPassword, cancellationToken);
        }
    }
}
