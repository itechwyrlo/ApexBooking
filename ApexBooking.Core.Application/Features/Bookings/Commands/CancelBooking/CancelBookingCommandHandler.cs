using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking;

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

    public async Task<BaseResponse<bool>> Handle(
        CancelBookingCommand request,
        CancellationToken cancellationToken)
    {
        var bookingId = new BookingId(request.BookingId);
        var userId = _userContextService.GetCurrentUserId();
        var tenantId = _userContextService.GetCurrentTenantId();
        var tenant = await _unitOfWork.TenantRepository.GetAsync(
            predicate: t => t.TenantId == tenantId,
            includes: t => t.TenantSettings
        );

        var booking = await _unitOfWork.BookingRepository.GetByIdAsync(bookingId);
        if (booking is null)
            return BaseResponse<bool>.Failure("Booking not found.");

        if (tenant.TenantSettings is null)
            return BaseResponse<bool>.Failure("Tenant settings not found.");

        // UC-3.2.4 Step 3: check cancellation window.
        var scheduledDateTime = booking.ScheduledDate.ToDateTime(booking.ScheduledStartTime);
        var hoursUntilBooking = (scheduledDateTime - DateTime.UtcNow).TotalHours;

        if (hoursUntilBooking < tenant.TenantSettings.CancellationCutoffHours)
            return BaseResponse<bool>.Failure(
                $"Cancellation is not allowed within {tenant.TenantSettings.CancellationCutoffHours} hours of the scheduled time.",
                "CANCELLATION_CUTOFF_EXCEEDED");

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