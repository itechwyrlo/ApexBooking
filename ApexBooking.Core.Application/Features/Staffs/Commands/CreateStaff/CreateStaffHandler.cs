using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.CreateStaff
{
    public class CreateResourceHandler : ICommandHandler<CreateStaffCommand, BaseResponse<Staff>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public CreateResourceHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<Staff>> Handle(CreateStaffCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();

            var tenant = await _unitOfWork.TenantRepository.GetAsync(t => t.TenantId == tenantId);
            if (tenant is null)
                return BaseResponse<Staff>.Failure("Tenant not found.");

            var limit = PlanLimits.MaxResources(tenant.Plan);
            if (limit.HasValue)
            {
                var existing = await _unitOfWork.StaffRepository.GetAllAsync();
                if (existing.Count() >= limit.Value)
                    throw new BusinessRuleBrokenException(
                        $"Your {tenant.Plan} plan allows a maximum of {limit.Value} resources.");
            }

            var staff = Staff.Create(
                tenantId: tenantId,
                firstname: command.FirstName,
                lastname: command.LastName,
                email: command.email,
                contactNumber: command.contactNumber,
                capacity: command.Capacity,
                description: command.Description
            );

            _unitOfWork.StaffRepository.Add(staff);
            await _unitOfWork.CompleteAsync(ct);
            return BaseResponse<Staff>.Success(staff);
        }
    }
}
