using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Policies;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using MediatR;

namespace ApexBooking.Core.Application.Features.TenantRequest.Events
{
    public class ProvisionTenantOnRequestApprovedHandler
        : INotificationHandler<DomainEventNotification<TenantRequestApproveDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISlugValidationPolicy _slugValidationPolicy;
        private readonly IApplicationUserService _applicationUserService;

        public ProvisionTenantOnRequestApprovedHandler(
            IUnitOfWork unitOfWork,
            ISlugValidationPolicy slugValidationPolicy,
            IApplicationUserService applicationUserService)
        {
            _unitOfWork = unitOfWork;
            _slugValidationPolicy = slugValidationPolicy;
            _applicationUserService = applicationUserService;
        }

        public async Task Handle(
    DomainEventNotification<TenantRequestApproveDomainEvent> notification,
    CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            // 1. Initial Identity Check
            var userExists = await _applicationUserService.ValidateUserByEmailAsync(e.OwnerEmail);
            if (userExists)
                throw new BusinessRuleBrokenException("Failed to provision Tenant, User already existing");
            var result = await _applicationUserService.CreatedUserAsync(e.OwnerEmail, e.OwnerFirstName, e.OwnerLastName);
            if (!result.IsSucceeded)
            {

                throw new BusinessRuleBrokenException("Failed to provision Tenant, Failed to create User");
            }
            var tenant = Tenant.Create(
                 e.RequestedSlug,
                 new OwnerContact(e.OwnerFirstName, e.OwnerLastName, e.OwnerEmail),
                 e.RequestedPlan,
                 e.BusinessName,
                 e.BusinessType,
                 _slugValidationPolicy,
                 e.ApprovedAt);

            _unitOfWork.TenantRepository.Add(tenant);

            // A freshly-provisioned tenant has zero branches — PrimaryBranchId (and InviteMember,
            // which requires one) would throw on an empty collection otherwise. Address/timezone
            // are placeholders (Address.Empty is a first-class "not filled in yet" value; Asia/Manila
            // mirrors this codebase's own canonical example zone) — the owner corrects both via
            // UpdateBranchProfile once they log in and complete setup (SetupRequired stays true).
            var primaryBranch = tenant.AddBranch("Main Branch", "Asia/Manila", Address.Empty);

            tenant.InviteMember(
                applicationUserId: result.Id,
                branchId: primaryBranch.BranchId,
                firstName: e.OwnerFirstName,
                lastName: e.OwnerLastName,
                email: e.OwnerEmail,
                assignedRole: SystemRole.Owner,
                description: "Primary Business Owner"
            );

            await _unitOfWork.CompleteAsync(cancellationToken);
        }

    }
}
