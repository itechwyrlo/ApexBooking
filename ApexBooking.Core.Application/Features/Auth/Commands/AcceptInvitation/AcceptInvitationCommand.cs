using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.AcceptInvitation;

public sealed record AcceptInvitationCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword
) : ICommand<AuthResponseDto>;
 
