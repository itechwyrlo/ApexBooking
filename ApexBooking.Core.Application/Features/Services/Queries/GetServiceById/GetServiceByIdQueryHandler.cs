using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Services.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetServiceById;

internal sealed class GetServiceByIdQueryHandler : IQueryHandler<GetServiceByIdQuery, ServiceDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _contextService;

    public GetServiceByIdQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
    }

    public async Task<ServiceDto> Handle(GetServiceByIdQuery query, CancellationToken ct)
    {
        var service = await _unitOfWork.ServiceRepository
            .GetAsync(
                predicate: s => s.ServiceId == new ServiceId(query.ServiceId),
                includes: t => t.ServiceStaffs);

        if (service is null)
            throw new NotFoundException("Service not found.");

        return service.ToServiceDto();
    }
}