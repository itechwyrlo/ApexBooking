using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.CreateOrganization;

internal sealed class CreateOrganizationCommandHandler
    : ICommandHandler<CreateOrganizationCommand, OrganizationSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrganizationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrganizationSummaryDto> Handle(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var slugExists = await _unitOfWork.TenantRepository.FindBySlugAsync(command.Slug);
        Tenant.EnsureSlugIsAvailable(slugExists is not null);

        var emailExistsAsTenant = await _unitOfWork.TenantRepository.FindByEmailAsync(command.OwnerEmail);
        Tenant.EnsureOwnerEmailIsAvailable(emailExistsAsTenant is not null);

        var emailExistsAsUser = await _unitOfWork.UserRepository.FindByEmailAcrossAllTenantsAsync(command.OwnerEmail);
        var tenant = Tenant.Create(
            command.Slug,
            command.BusinessName,
            command.OwnerFullName,
            command.OwnerEmail,
            command.OwnerPhone
        );
        tenant.EnsureUserEmailIsNotRegistered(emailExistsAsUser is not null);

        tenant.CreateTenantProfile("UTC", "USD");
        tenant.CreateTenantSettings();
        tenant.CreateTenantPaymentPolicy();
        tenant.MarkAsVerified();

        var admin = User.Create(
            tenant.TenantId,
            command.OwnerFullName,
            command.OwnerEmail,
            UserRole.TenantAdmin
        );

        _unitOfWork.TenantRepository.Add(tenant);
        await _unitOfWork.CompleteAsync(cancellationToken);

        var createResult = await _unitOfWork.UserRepository.CreateAsync(admin, command.AdminPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin user: {errors}");
        }

        var roleResult = await _unitOfWork.UserRepository.AddToRoleAsync(admin, "TENANT ADMIN");
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign admin role: {errors}");
        }

        admin = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, admin.Email!);
        if (admin is null)
            throw new NotFoundException("Error retrieving created admin user.");

        admin.MarkEmailVerified();
        await _unitOfWork.CompleteAsync(cancellationToken);

        return tenant.ToOrganizationSummaryDto(userCount: 1);
    }
}
