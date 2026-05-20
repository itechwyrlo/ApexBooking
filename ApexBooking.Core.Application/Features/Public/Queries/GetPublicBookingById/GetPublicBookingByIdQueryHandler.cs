using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicBookingById
{
    internal sealed class GetPublicBookingQueryHandler
        : IQueryHandler<GetPublicBookingByIdQuery, PublicBookingDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPublicBookingQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PublicBookingDto> Handle(
            GetPublicBookingByIdQuery query,
            CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);
            if (tenant is null)
                throw new NotFoundException("Booking not found.");

            var booking = await _unitOfWork.BookingRepository.GetAsync(
                b => b.BookingId == new BookingId(query.BookingId),
                b => b.Guest);

            if (booking is null)
                throw new NotFoundException("Booking not found.");

            return booking.ToPublicDto();
        }
    }
}