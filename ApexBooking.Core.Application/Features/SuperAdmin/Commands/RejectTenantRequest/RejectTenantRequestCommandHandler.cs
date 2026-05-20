using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.RejectTenantRequest;

internal sealed class RejectTenantRequestCommandHandler : ICommandHandler<RejectTenantRequestCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthNotificationService _authNotification;

    public RejectTenantRequestCommandHandler(IUnitOfWork unitOfWork, IAuthNotificationService authNotification)
    {
        _unitOfWork = unitOfWork;
        _authNotification = authNotification;
    }

    public async Task Handle(RejectTenantRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _unitOfWork.TenantRequestRepository.GetByIdAsync(command.RequestId);
        if (request is null)
            throw new NotFoundException("Request not found.");

        request.Reject(command.Reason);
        _unitOfWork.TenantRequestRepository.Update(request);

        var superAdmins = await _unitOfWork.SuperAdminRepository.GetAllAsync(sa => sa.IsActive);
        foreach (var sa in superAdmins)
        {
            _unitOfWork.NotificationRepository.Add(Notification.Create(
                sa.SuperAdminId.Value,
                NotificationRecipientType.SuperAdmin,
                null,
                NotificationEventType.TenantRequestRejected,
                "Tenant Request Rejected",
                $"Tenant request from {request.BusinessName} has been rejected."));
        }

        await _unitOfWork.CompleteAsync(cancellationToken);

        await _authNotification.SendRejectionEmailAsync(
            request.OwnerEmail,
            request.OwnerFullName,
            request.BusinessName,
            command.Reason,
            cancellationToken);
    }
}
