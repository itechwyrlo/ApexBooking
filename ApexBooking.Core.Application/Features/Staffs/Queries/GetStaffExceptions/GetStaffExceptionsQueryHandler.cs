using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffExceptions;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetResourceExceptions;

internal sealed class GetStaffExceptionsQueryHandler
    : IQueryHandler<GetStaffExceptionsQuery, PagedResult<StaffAvailabilityExceptionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetStaffExceptionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<StaffAvailabilityExceptionDto>> Handle(
        GetStaffExceptionsQuery query,
        CancellationToken cancellationToken)
    {

        var pagedResources = await _unitOfWork.StaffRepository.GetPageAsync(
        query.param,
        predicate: r => r.StaffId == new StaffId(query.StaffId),
        includes: r => r.AvailabilityExceptions);

        var allExceptions = pagedResources.data
            .SelectMany(r => r.AvailabilityExceptions)
            .ToList();

        var dtos = allExceptions
            .Select(ex => ex.ToExceptionDto())
            .ToList();

        return new PagedResult<StaffAvailabilityExceptionDto>(dtos, allExceptions.Count);

    }
}