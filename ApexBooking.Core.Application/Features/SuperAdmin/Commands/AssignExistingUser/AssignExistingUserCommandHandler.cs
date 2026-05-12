using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.AssignExistingUser;

internal sealed class AssignExistingUserCommandHandler
    : ICommandHandler<AssignExistingUserCommand, BaseResponse<TenantUserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public AssignExistingUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<TenantUserDto>> Handle(
        AssignExistingUserCommand command,
        CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(command.TenantSlug);
        if (tenant is null)
            return BaseResponse<TenantUserDto>.Failure("Organization not found.");

        var user = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, command.Email);
        if (user is null)
            return BaseResponse<TenantUserDto>.Failure("No user with this email found in this organization.");

        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var newRole))
            return BaseResponse<TenantUserDto>.Failure($"Invalid role '{command.Role}'.");

        if (user.Role == newRole)
            return BaseResponse<TenantUserDto>.Failure($"User already has the '{command.Role}' role.");

        user.ChangeRole(newRole);
        await _unitOfWork.CompleteAsync(cancellationToken);

        return BaseResponse<TenantUserDto>.Success(user.ToTenantUserDto());
    }
}
