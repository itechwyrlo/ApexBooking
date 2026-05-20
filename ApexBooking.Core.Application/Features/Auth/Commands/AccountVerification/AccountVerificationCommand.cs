using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.AccountVerification;

public sealed record AccountVerificationCommand(
    string token,
    string? ReturnTo = null
) : ICommand<AccountVerificationResponseDto>;