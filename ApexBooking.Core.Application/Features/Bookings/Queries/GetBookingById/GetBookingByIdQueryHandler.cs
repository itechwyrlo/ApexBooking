using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetBookingById
{
    internal sealed class GetBookingByIdQueryHandler : IQueryHandler<GetBookingByIdQuery, BaseResponse<BookingDetailDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetBookingByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<BookingDetailDto>> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
        {
            var bookingId = new BookingId(request.BookingId);

            var booking = await _unitOfWork.BookingRepository.GetAsync(
                predicate: b => b.BookingId == bookingId,
                b => b.Guest);

            if (booking is null)
                return BaseResponse<BookingDetailDto>.Failure("Booking not found");

            var dto = booking.ToDetailDto(booking.ServiceName, booking.ResourceName);

            return BaseResponse<BookingDetailDto>.Success(dto);
        }
    }
}
