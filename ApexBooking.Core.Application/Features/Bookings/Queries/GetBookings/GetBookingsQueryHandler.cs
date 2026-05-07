using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetBookings;

internal sealed class GetBookingsQueryHandler
    : IQueryHandler<GetBookingsQuery, BaseResponse<List<BookingDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetBookingsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<List<BookingDto>>> Handle(
        GetBookingsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.BookingRepository
            .GetPageAsync(
                new QueryObjectParams { PageNumber = 1, PageSize = int.MaxValue })
            .ConfigureAwait(false);

        var dtos = result.data.Select(b => new BookingDto(
            b.BookingId.Value,
            b.BookingReference,
            b.ServiceId.Value,
            string.Empty,
            b.ResourceId.Value,
            string.Empty,
            b.UserId,
            b.ScheduledDate,
            b.ScheduledStartTime,
            b.ScheduledEndTime,
            b.DurationMinutes,
            b.Status,
            b.ConfirmationMode,
            b.PriceSnapshot,
            b.CurrencyCode,
            b.CustomerNotes,
            b.CancellationReason,
            b.CancelledAt,
            b.CreatedAt
        ));

        return BaseResponse<List<BookingDto>>.Success(dtos.ToList());
    }
}