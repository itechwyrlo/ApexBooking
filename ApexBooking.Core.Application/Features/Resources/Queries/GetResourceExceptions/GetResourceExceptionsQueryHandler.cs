using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Resources.Queries.GetResourceExceptions;

internal sealed class GetResourceExceptionsQueryHandler
    : IQueryHandler<GetResourceExceptionsQuery, PagedResult<ResourceAvailabilityExceptionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetResourceExceptionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ResourceAvailabilityExceptionDto>> Handle(
        GetResourceExceptionsQuery query,
        CancellationToken cancellationToken)
    {

        var pagedResources = await _unitOfWork.ResourceRepository.GetPageAsync(
        query.param,
        predicate: r => r.ResourceId == new ResourceId(query.ResourceId),
        includes: r => r.AvailabilityExceptions);

        var allExceptions = pagedResources.data
            .SelectMany(r => r.AvailabilityExceptions)
            .ToList();

        var dtos = allExceptions
            .Select(ex => ex.ToExceptionDto())
            .ToList();

        return new PagedResult<ResourceAvailabilityExceptionDto>(dtos, allExceptions.Count);

    }
}