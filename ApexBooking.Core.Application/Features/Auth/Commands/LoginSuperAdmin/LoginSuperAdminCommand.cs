using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Auth.Commands.LoginSuperAdmin;

public sealed record LoginSuperAdminCommand(string Email, string Password)
    : ICommand<BaseResponse<AuthResponseDto>>;
