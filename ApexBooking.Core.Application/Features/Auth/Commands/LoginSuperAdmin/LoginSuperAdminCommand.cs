using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.LoginSuperAdmin;

public sealed record LoginSuperAdminCommand(string Email, string Password)
    : ICommand<AuthResponseDto>;
