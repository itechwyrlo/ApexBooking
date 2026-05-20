using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.AssignExistingUser;

internal sealed class AssignExistingUserCommandHandler
    : ICommandHandler<AssignExistingUserCommand, TenantUserDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AssignExistingUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantUserDto> Handle(
        AssignExistingUserCommand command,
        CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(command.TenantSlug);
        if (tenant is null)
            throw new NotFoundException("Organization not found.");

        var user = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, command.Email);
        if (user is null)
            throw new NotFoundException("No user with this email found in this organization.");

        var newRole = Enum.Parse<UserRole>(command.Role, ignoreCase: true);

        user.ChangeRole(newRole);
        await _unitOfWork.CompleteAsync(cancellationToken);

        return user.ToTenantUserDto();
    }
}
