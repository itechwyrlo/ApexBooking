using System;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.Branches
{
    public class AddBranchHandler : ICommandHandler<AddBranchCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public AddBranchHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<Guid> Handle(AddBranchCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to add branch. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Branches);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to add branch. Isolated business workspace could not be verified.");

            var address = new Address(
                command.Street,
                command.Barangay,
                command.City,
                command.Province,
                command.ZipCode
            );

            var branch = tenant.AddBranch(command.BranchName, command.TimeZoneId, address);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return branch.BranchId.Value;
        }
    }
}
