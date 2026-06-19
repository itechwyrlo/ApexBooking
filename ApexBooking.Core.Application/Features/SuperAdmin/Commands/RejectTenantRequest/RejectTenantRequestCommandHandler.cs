using ApexBooking.Core.Application.Interfaces;
using ApexBooking.Core.Application.mapper;
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
    private readonly IPushNotificationService _pushService;
    private readonly IRealtimeNotificationService _realtimeService;

    public RejectTenantRequestCommandHandler(
        IUnitOfWork unitOfWork,
        IAuthNotificationService authNotification,
        IPushNotificationService pushService,
        IRealtimeNotificationService realtimeService)
    {
        _unitOfWork = unitOfWork;
        _authNotification = authNotification;
        _pushService = pushService;
        _realtimeService = realtimeService;
    }

    public async Task Handle(RejectTenantRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _unitOfWork.TenantRequestRepository.GetByIdAsync(command.RequestId);
        if (request is null)
            throw new NotFoundException("Request not found.");

        request.Reject(command.Reason);
        _unitOfWork.TenantRequestRepository.Update(request);

        var superAdmins = await _unitOfWork.SuperAdminRepository.GetAllAsync(sa => sa.IsActive);
        var superAdminNotifications = new List<Notification>();
        foreach (var sa in superAdmins)
        {
            var n = Notification.Create(
                sa.SuperAdminId.Value,
                NotificationRecipientType.SuperAdmin,
                null,
                NotificationEventType.TenantRequestRejected,
                "Tenant Request Rejected",
                $"Tenant request from {request.BusinessName} has been rejected.");
            _unitOfWork.NotificationRepository.Add(n);
            superAdminNotifications.Add(n);
        }

        await _unitOfWork.CompleteAsync(cancellationToken);

        await _pushService.SendAsync(
            superAdminNotifications.Select(n => n.RecipientId).ToList(),
            "Tenant Request Rejected",
            $"Tenant request from {request.BusinessName} has been rejected.",
            NotificationEventType.TenantRequestRejected.ToString(),
            cancellationToken);
        await Task.WhenAll(superAdminNotifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));

        await _authNotification.SendRejectionEmailAsync(
            request.OwnerEmail,
            request.OwnerFullName,
            request.BusinessName,
            command.Reason,
            cancellationToken);
    }
}
