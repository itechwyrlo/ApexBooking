using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking
{
    internal sealed class CancelBookingCommandHandler : ICommandHandler<CancelBookingCommand, BaseResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContextService;

        public CancelBookingCommandHandler(
            IUnitOfWork unitOfWork,
            IUserContextService userContextService)
        {
            _unitOfWork = unitOfWork;
            _userContextService = userContextService;
        }

        public async Task<BaseResponse<bool>> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
        {
            var bookingId = new BookingId(request.BookingId);
            var userId = _userContextService.GetCurrentUserId();

            var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
            if (booking is null)
                return BaseResponse<bool>.Failure("Booking not found");

            try
            {
                booking.Cancel(userId, request.Reason);
            }
            catch (BusinessRuleBrokenException ex)
            {
                return BaseResponse<bool>.Failure(ex.Message);
            }

            _unitOfWork.BookingRepository.Update(booking);

            await _unitOfWork.CompleteAsync(cancellationToken);

            return BaseResponse<bool>.Success(true);
        }
    }
}