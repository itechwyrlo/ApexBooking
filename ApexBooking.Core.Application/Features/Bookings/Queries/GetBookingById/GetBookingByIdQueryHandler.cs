using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetBookingById
{
    internal sealed class GetBookingByIdQueryHandler : IQueryHandler<GetBookingByIdQuery, BaseResponse<BookingDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetBookingByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<BookingDto>> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
        {
            var bookingId = new BookingId(request.BookingId);

            var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
            if (booking is null)
                return BaseResponse<BookingDto>.Failure("Booking not found");

            var dto = new BookingDto(
                booking.BookingId.Value,
                booking.BookingReference,
                booking.ServiceId.Value,
                string.Empty,
                booking.ResourceId.Value,
                string.Empty,
                booking.UserId,
                booking.ScheduledDate,
                booking.ScheduledStartTime,
                booking.ScheduledEndTime,
                booking.DurationMinutes,
                booking.Status,
                booking.ConfirmationMode,
                booking.PriceSnapshot,
                booking.CurrencyCode,
                booking.CustomerNotes,
                booking.CancellationReason,
                booking.CancelledAt,
                booking.CreatedAt
            );

            return BaseResponse<BookingDto>.Success(dto);
        }
    }
}