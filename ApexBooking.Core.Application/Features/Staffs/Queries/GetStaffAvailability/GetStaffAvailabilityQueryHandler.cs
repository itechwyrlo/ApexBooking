using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffAvailability;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetStaff
{
    internal sealed class GetStaffAvailabilityQueryHandler
         : IQueryHandler<GetStaffAvailabilityQuery, BaseResponse<StaffAvailabilityDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetStaffAvailabilityQueryHandler(
            IUnitOfWork unitOfWork,
            IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<StaffAvailabilityDto>> Handle(
            GetStaffAvailabilityQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var staffId = new StaffId(query.StaffId);

            var staff = await _unitOfWork.StaffRepository
                .FindByIdWithAvailabilityAsync(staffId, cancellationToken);

            if (staff is null || staff.TenantId != tenantId)
                return BaseResponse<StaffAvailabilityDto>.Failure("Staff not found.");

            return BaseResponse<StaffAvailabilityDto>.Success(staff.ToAvailabilityDto());
        }
    }
}