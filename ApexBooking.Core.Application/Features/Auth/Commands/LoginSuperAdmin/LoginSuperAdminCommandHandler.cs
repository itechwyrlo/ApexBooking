using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Models;
using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Application.Features.Auth.Commands.LoginSuperAdmin;

internal sealed class LoginSuperAdminCommandHandler
    : ICommandHandler<LoginSuperAdminCommand, BaseResponse<AuthResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public LoginSuperAdminCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<BaseResponse<AuthResponseDto>> Handle(
        LoginSuperAdminCommand command,
        CancellationToken ct)
    {
        var superAdmin = await _unitOfWork.SuperAdminRepository.FindByEmailAsync(command.Email);
        if (superAdmin is null)
            return BaseResponse<AuthResponseDto>.Failure("Invalid email or password.");

        if (!superAdmin.IsActive)
            return BaseResponse<AuthResponseDto>.Failure("Account is not active.");

        var hasher = new PasswordHasher<Domain.Entities.SuperAdmin>();
        var result = hasher.VerifyHashedPassword(superAdmin, superAdmin.PasswordHash, command.Password);
        if (result == PasswordVerificationResult.Failed)
            return BaseResponse<AuthResponseDto>.Failure("Invalid email or password.");

        superAdmin.UpdateLastLogin();
        _unitOfWork.SuperAdminRepository.Update(superAdmin);
        await _unitOfWork.CompleteAsync(ct);

        var accessToken = _tokenService.GenerateAccessToken(superAdmin);

        return BaseResponse<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = accessToken,
            UserId = superAdmin.SuperAdminId.Value,
            RefreshToken = null!,
            TenantId = null!
        });
    }
}
