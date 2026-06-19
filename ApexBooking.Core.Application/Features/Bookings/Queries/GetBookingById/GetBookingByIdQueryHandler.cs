using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetBookingById
{
    internal sealed class GetBookingByIdQueryHandler : IQueryHandler<GetBookingByIdQuery, BookingDetailDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetBookingByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BookingDetailDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
        {
            var bookingId = new BookingId(request.BookingId);

            var booking = await _unitOfWork.BookingRepository.GetAsync(
                predicate: b => b.BookingId == bookingId,
                b => b.Guest);

            if (booking is null)
                throw new NotFoundException("Booking not found.");

            return booking.ToDetailDto(booking.ServiceName, booking.ResourceName);
        }
    }
}
