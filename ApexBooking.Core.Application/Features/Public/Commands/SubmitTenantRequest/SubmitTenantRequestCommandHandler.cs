using ApexBooking.Core.Application.Interfaces;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Auth;

namespace ApexBooking.Core.Application.Features.Public.Commands.SubmitTenantRequest;

internal sealed class SubmitTenantRequestCommandHandler
    : ICommandHandler<SubmitTenantRequestCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthNotificationService _authNotification;
    private readonly IPushNotificationService _pushService;
    private readonly IRealtimeNotificationService _realtimeService;

    public SubmitTenantRequestCommandHandler(
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

    public async Task<Guid> Handle(
        SubmitTenantRequestCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.TenantRequestRepository.GetAllAsync(
            r => r.OwnerEmail == command.OwnerEmail &&
                 r.Status == TenantRequestStatus.Pending);

        TenantRequest.EnsureNoPendingRequest(existing.Any());

        var plan = Enum.Parse<TenantPlan>(command.Plan, ignoreCase: true);

        var request = TenantRequest.Create(
            command.BusinessName,
            command.OwnerFullName,
            command.OwnerEmail,
            command.OwnerPhone,
            plan,
            command.Message);

        _unitOfWork.TenantRequestRepository.Add(request);

        var superAdmins = await _unitOfWork.SuperAdminRepository.GetAllAsync(sa => sa.IsActive);
        var superAdminNotifications = new List<Notification>();
        foreach (var sa in superAdmins)
        {
            var n = Notification.Create(
                sa.SuperAdminId.Value,
                NotificationRecipientType.SuperAdmin,
                null,
                NotificationEventType.TenantRequestSubmitted,
                "New Tenant Request",
                $"New tenant request from {request.BusinessName} is awaiting your approval.");
            _unitOfWork.NotificationRepository.Add(n);
            superAdminNotifications.Add(n);
        }

        await _unitOfWork.CompleteAsync(cancellationToken);

        await _pushService.SendAsync(
            superAdminNotifications.Select(n => n.RecipientId).ToList(),
            "New Tenant Request",
            $"New tenant request from {request.BusinessName} is awaiting your approval.",
            NotificationEventType.TenantRequestSubmitted.ToString(),
            cancellationToken);
        await Task.WhenAll(superAdminNotifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));

        await _authNotification.SendTenantRequestReceivedEmailAsync(
            command.OwnerEmail,
            command.OwnerFullName,
            command.BusinessName,
            plan.ToString(),
            cancellationToken);

        return request.Id.Value;
    }
}
