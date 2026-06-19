using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffExceptions;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetStaff
{
    internal sealed class GetStaffQueryHandler
    : IQueryHandler<GetStaffQuery, PagedResult<StaffDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetStaffQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<StaffDto>> Handle(
            GetStaffQuery query,
            CancellationToken cancellationToken)
        {
            var pagedResult = await _unitOfWork.StaffRepository.GetPageAsync(query.param);
            var resources = pagedResult.data.Select(r => r.ToStaffDto()).ToList();
      
            return new PagedResult<StaffDto>(resources, pagedResult.total);
        }
    }
}