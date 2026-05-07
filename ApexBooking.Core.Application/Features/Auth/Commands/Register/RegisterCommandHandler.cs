using System.Buffers.Text;
using System.Text;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace ApexBooking.Core.Application.Features.Auth.Commands.Register;

internal sealed class RegisterCommandHandler : ICommandHandler<RegisterAdminCommand, BaseResponse<AuthResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly INotificationService _notification;
    private readonly IConfiguration _configuration;

    public RegisterCommandHandler(
        ITokenService tokenService,
        INotificationService notification,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _notification = notification;
        _configuration = configuration;
    }

    public async Task<BaseResponse<AuthResponseDto>> Handle(RegisterAdminCommand command, CancellationToken cancellationToken)
    {
        var slug = command.OrganizationName.ToLowerInvariant();

        // =========================
        // VALIDATION
        // =========================

        var existingTenant = await _unitOfWork.TenantRepository.FindBySlugAsync(slug);
        if (existingTenant != null)
            return BaseResponse<AuthResponseDto>.Failure("Organization name is already taken.");

        var existingUser = await _unitOfWork.UserRepository.FindByEmailAcrossAllTenantsAsync(command.Email);
        if (existingUser != null)
            return BaseResponse<AuthResponseDto>.Failure("Email is already registered.");

        // =========================
        // CREATE AGGREGATES
        // =========================

        var tenant = Tenant.Create(
            slug,
            command.OrganizationName,
            $"{command.FirstName} {command.LastName}",
            command.Email,
            command.Phone
        );

        tenant.CreateTenantProfile("UTC", "USD");
        tenant.CreateTenantSettings();

        var admin = User.Create(
            tenant.TenantId,
            $"{command.FirstName} {command.LastName}",
            command.Email,
            UserRole.TenantAdmin
        );

        _unitOfWork.TenantRepository.Add(tenant);
        await _unitOfWork.CompleteAsync(cancellationToken);

        var createResult = await _unitOfWork.UserRepository.CreateAsync(admin, command.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return BaseResponse<AuthResponseDto>.Failure($"Failed to create user: {errors}");
        }

        var roleResult = await _unitOfWork.UserRepository.AddToRoleAsync(admin, "TENANT ADMIN");
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            return BaseResponse<AuthResponseDto>.Failure($"Failed to assign role: {errors}");
        }

        admin = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, admin.Email);
        if(admin == null) return BaseResponse<AuthResponseDto>.Failure("Error, User not found after creation.");

        var verificationToken = await _unitOfWork.UserRepository.GenerateEmailConfirmationTokenAsync(admin
        );

        var tokenBytes = Encoding.UTF8.GetBytes(verificationToken);
        var urlSafeToken = Base64Url.EncodeToString(tokenBytes);

        admin.AddConfirmationEmailToken(verificationToken);

        var accessToken = _tokenService.GenerateAccessToken(admin, "TENANT ADMIN", tenant.Slug);
        var refreshToken = _tokenService.GenerateRefreshTokenRaw();

        admin.AddRefreshToken(refreshToken, DateTime.UtcNow);

        await _unitOfWork.CompleteAsync(cancellationToken);

        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5096";
        var verificationUrl =
            $"{baseUrl.Replace("localhost:5096", $"{tenant.Slug}.localhost:5096")}/verify-account?token={urlSafeToken}";
        var emailContent =
            $"<p>Click <a href='{verificationUrl}'>here</a> to verify your email address.</p><p>This link expires in 24 hours.</p>";

        await _notification.SendEmailAsync(admin.Email!, "Verify your email address", emailContent);

        return BaseResponse<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = admin.Id,
            TenantId = tenant.TenantId
        });
    }
}