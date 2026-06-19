using System;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffAvailability;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetStaff
{
    internal sealed class GetStaffAvailabilityQueryHandler
         : IQueryHandler<GetStaffAvailabilityQuery, StaffAvailabilityDto>
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

        public async Task<StaffAvailabilityDto> Handle(
            GetStaffAvailabilityQuery query,
            CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var staffId = new StaffId(query.StaffId);

            var staff = await _unitOfWork.StaffRepository
                .FindByIdWithAvailabilityAsync(staffId, cancellationToken);

            if (staff is null || staff.TenantId != tenantId)
                throw new NotFoundException("Staff not found.");

            return staff.ToAvailabilityDto();
        }
    }
}