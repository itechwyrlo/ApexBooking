using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.AssignExistingUser;

public sealed record AssignExistingUserCommand(
    string TenantSlug,
    string Email,
    string Role
) : ICommand<BaseResponse<TenantUserDto>>;
