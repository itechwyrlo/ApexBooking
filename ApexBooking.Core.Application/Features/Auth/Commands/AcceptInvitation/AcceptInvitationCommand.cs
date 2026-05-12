using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Auth.Commands.AcceptInvitation;

public sealed record AcceptInvitationCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword
) : ICommand<BaseResponse<AuthResponseDto>>;
