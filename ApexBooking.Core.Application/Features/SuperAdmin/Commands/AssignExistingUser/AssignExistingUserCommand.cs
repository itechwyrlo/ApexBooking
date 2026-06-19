using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.AssignExistingUser;

public sealed record AssignExistingUserCommand(
    string TenantSlug,
    string Email,
    string Role
) : ICommand<TenantUserDto>;
