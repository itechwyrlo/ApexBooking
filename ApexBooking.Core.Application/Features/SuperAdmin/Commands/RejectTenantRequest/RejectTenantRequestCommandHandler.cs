using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.RejectTenantRequest;

internal sealed class RejectTenantRequestCommandHandler
    : ICommandHandler<RejectTenantRequestCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notification;

    public RejectTenantRequestCommandHandler(IUnitOfWork unitOfWork, INotificationService notification)
    {
        _unitOfWork = unitOfWork;
        _notification = notification;
    }

    public async Task<BaseResponse<bool>> Handle(
        RejectTenantRequestCommand command,
        CancellationToken cancellationToken)
    {
        var request = await _unitOfWork.TenantRequestRepository.GetByIdAsync(command.RequestId);
        if (request is null)
            return BaseResponse<bool>.Failure("Request not found.");

        request.Reject(command.Reason);
        _unitOfWork.TenantRequestRepository.Update(request);
        await _unitOfWork.CompleteAsync(cancellationToken);

        var emailBody = $@"
            <p>Hi {request.OwnerFullName},</p>
            <p>Thank you for your interest in ApexBooking.</p>
            <p>After reviewing your request for <strong>{request.BusinessName}</strong>, we are unable to approve your account at this time.</p>
            <p><strong>Reason:</strong> {command.Reason}</p>
            <p>If you believe this is an error or would like to discuss further, please contact our support team.</p>";

        await _notification.SendEmailAsync(
            request.OwnerEmail,
            "Your ApexBooking access request",
            emailBody);

        return BaseResponse<bool>.Success(true);
    }
}
