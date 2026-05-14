using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffById;

namespace ApexBooking.Core.Application.Features.Resources.Queries.GetResourceById
{
    internal sealed class GetResourceByIdQueryHandler
     : IQueryHandler<GetStaffByIdQuery, BaseResponse<StaffDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public GetResourceByIdQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task<BaseResponse<StaffDto>> Handle(
            GetStaffByIdQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var staffId = new ResourceId(query.StaffId);

            var staff = await _unitOfWork.StaffRepository
                .GetByIdAsync(staffId)
                .ConfigureAwait(false);

            if (staff is null || staff.TenantId != tenantId)
                return BaseResponse<StaffDto>.Failure("Staff not found.");

            

            return BaseResponse<StaffDto>.Success(staff.ToStaffDto());
        }
    }
}