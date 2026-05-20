using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.CreateTenantUser;

public sealed record CreateTenantUserCommand(
    string TenantSlug,
    string FullName,
    string Email,
    string Role
) : ICommand<TenantUserDto>;
