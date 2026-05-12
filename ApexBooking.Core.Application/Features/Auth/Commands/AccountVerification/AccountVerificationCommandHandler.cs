using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Auth.Commands.AccountVerification
{
    internal sealed class AccountVerificationCommandHandler : ICommandHandler<AccountVerificationCommand, BaseResponse<AccountVerificationResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        public AccountVerificationCommandHandler(IUserContextService userContext, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;

        }
        public async Task<BaseResponse<AccountVerificationResponseDto>> Handle(AccountVerificationCommand command, CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(command.token))
                return BaseResponse<AccountVerificationResponseDto>.Failure("Verification token is required.");

            byte[] decodedBytes = Base64Url.DecodeFromChars(command.token);
            string originalToken = Encoding.UTF8.GetString(decodedBytes);

            var user = await _unitOfWork.UserRepository.GetUserByEmailTokenAsync(originalToken);

            // Verify the token       
            var tokenIsValid = await _unitOfWork.UserRepository.ConfirmEmailAsync(user, originalToken);


            if (!tokenIsValid.Succeeded)
                return BaseResponse<AccountVerificationResponseDto>.Failure(string.Join(", ", tokenIsValid.Errors.Select(x => x.Description)));

            // Check if token has expired
            if (user.InvitationExpiresAt.HasValue && user.InvitationExpiresAt < DateTime.UtcNow)
                return BaseResponse<AccountVerificationResponseDto>.Failure("Verification token has expired.");

            // Get tenant and activate
            var tenant = await _unitOfWork.TenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null)
                return BaseResponse<AccountVerificationResponseDto>.Failure("Tenant not found.");

            // Mark email as verified using domain methods
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

            return BaseResponse<AccountVerificationResponseDto>.Success(new AccountVerificationResponseDto
            {
                Url = redirectUrl,
                TenantSlug = tenant.Slug
            });
        }
    }
}