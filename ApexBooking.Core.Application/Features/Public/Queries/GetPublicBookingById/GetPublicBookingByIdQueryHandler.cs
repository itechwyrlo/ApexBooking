using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;
using System.Threading;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicBookingById
{
    internal sealed class GetPublicBookingQueryHandler 
        : IQueryHandler<GetPublicBookingByIdQuery, BaseResponse<PublicBookingDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetPublicBookingQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<PublicBookingDto>> Handle(
            GetPublicBookingByIdQuery query, 
            CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);
            if (tenant is null)
            {
                return BaseResponse<PublicBookingDto>.Failure("Booking not found.");
            }

            // Using GetAsync with include for Guest as it belongs to the repository pattern
            var booking = await _unitOfWork.BookingRepository.GetAsync(
                b => b.BookingId == new BookingId(query.BookingId),
                b => b.Guest);

            if (booking is null)
            {
                return BaseResponse<PublicBookingDto>.Failure("Booking not found.");
            }

            return BaseResponse<PublicBookingDto>.Success(booking.ToPublicDto());
        }
    }
}