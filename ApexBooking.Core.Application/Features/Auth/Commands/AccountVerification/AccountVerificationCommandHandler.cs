using System.Buffers.Text;
using System.Text;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.AccountVerification;

internal sealed class AccountVerificationCommandHandler : ICommandHandler<AccountVerificationCommand, AccountVerificationResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public AccountVerificationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountVerificationResponseDto> Handle(AccountVerificationCommand command, CancellationToken cancellationToken)
    {
        byte[] decodedBytes = Base64Url.DecodeFromChars(command.token);
        string originalToken = Encoding.UTF8.GetString(decodedBytes);

        var user = await _unitOfWork.UserRepository.GetUserByEmailTokenAsync(originalToken);
        if (user is null)
            throw new UnauthorizedException("Invalid or expired verification token.");

        var tokenIsValid = await _unitOfWork.UserRepository.ConfirmEmailAsync(user, originalToken);
        if (!tokenIsValid.Succeeded)
            throw new UnauthorizedException("Invalid or expired verification token.");

        user.EnsureInvitationNotExpired();

        var tenant = await _unitOfWork.TenantRepository.GetByIdAsync(user.TenantId);
        if (tenant is null)
            throw new NotFoundException("Tenant not found.");

        user.MarkEmailVerified();
        if (user.Role == UserRole.TenantAdmin) tenant.MarkAsVerified();

        await _unitOfWork.CompleteAsync();

        string redirectUrl;

        if (user.Role == UserRole.Customer)
        {
            redirectUrl = $"/book/{tenant.Slug}/customer/login?verified=email";
            if (!string.IsNullOrWhiteSpace(command.ReturnTo))
                redirectUrl += $"&returnTo={Uri.EscapeDataString(command.ReturnTo)}";
        }
        else
        {
            redirectUrl = $"/login?verified=email&tenant={tenant.Slug}";
            if (!string.IsNullOrWhiteSpace(command.ReturnTo))
                redirectUrl += $"&returnTo={Uri.EscapeDataString(command.ReturnTo)}";
        }

        return new AccountVerificationResponseDto
        {
            Url = redirectUrl,
            TenantSlug = tenant.Slug
        };
    }
}