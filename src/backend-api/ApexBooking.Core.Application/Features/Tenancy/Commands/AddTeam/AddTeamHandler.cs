using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.AddTeam
{
    public class AddTeamHandler : ICommandHandler<AddTeamCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IApplicationUserService _applicationUserService;
        private readonly ITenantEntity _tenantEntity;

        public AddTeamHandler(IUnitOfWork unitOfWork, IApplicationUserService applicationUserService, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _applicationUserService = applicationUserService;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(AddTeamCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to add team. No authenticated tenant context was found.");

            var currentTenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Branches, t => t.Members]
            );

            if (currentTenant == null)
                throw new BusinessRuleBrokenException("Failed to add team. Isolated business workspace could not be verified.");

            var result = await _applicationUserService.CreatedUserAsync(command.request.email, command.request.firstName, command.request.lastName);
            if (!result.IsSucceeded)
            {
                var reason = result.Errors is not null && result.Errors.Any()
                    ? string.Join(" ", result.Errors)
                    : "Unknown error.";
                throw new BusinessRuleBrokenException($"Failed to create member user account: {reason}");
            }

           
            currentTenant.InviteMember(
                applicationUserId: result.Id,
                branchId: new BranchId(command.request.branchId),
                firstName: command.request.firstName,
                lastName: command.request.lastName,
                email: command.request.email,
                assignedRole: command.request.role,
                contactNumber: command.request.contactNumber ?? string.Empty,
                description: command.request.customJobTitle
            );

            _unitOfWork.TenantRepository.Update(currentTenant);
            await _unitOfWork.CompleteAsync();
        }
    }
}
