using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Application.mapper;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetBookings;

internal sealed class GetBookingsQueryHandler
    : IQueryHandler<GetBookingsQuery, PagedResult<TenantBookingsDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBookingsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<TenantBookingsDto>> Handle(
        GetBookingsQuery query,
        CancellationToken cancellationToken)
    {
        var pagedResult = await _unitOfWork.BookingRepository
            .GetPageAsync(query.param, predicate: null, b => b.Guest)
            .ConfigureAwait(false);

        var dtos = pagedResult.data
            .Select(b => b.ToTenantDto())
            .ToList();

        return new PagedResult<TenantBookingsDto>(dtos, pagedResult.total);
    }
}
