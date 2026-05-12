using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Public.Commands.SubmitTenantRequest;

internal sealed class SubmitTenantRequestCommandHandler
    : ICommandHandler<SubmitTenantRequestCommand, BaseResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notification;

    public SubmitTenantRequestCommandHandler(IUnitOfWork unitOfWork, INotificationService notification)
    {
        _unitOfWork = unitOfWork;
        _notification = notification;
    }

    public async Task<BaseResponse<Guid>> Handle(
        SubmitTenantRequestCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.TenantRequestRepository.GetAllAsync(
            r => r.OwnerEmail == command.OwnerEmail &&
                 r.Status == TenantRequestStatus.Pending);

        if (existing.Any())
            return BaseResponse<Guid>.Failure("A pending request for this email already exists.");

        if (!Enum.TryParse<TenantPlan>(command.Plan, ignoreCase: true, out var plan))
            return BaseResponse<Guid>.Failure($"Invalid plan '{command.Plan}'.");

        var request = TenantRequest.Create(
            command.BusinessName,
            command.OwnerFullName,
            command.OwnerEmail,
            command.OwnerPhone,
            plan,
            command.Message);

        _unitOfWork.TenantRequestRepository.Add(request);
        await _unitOfWork.CompleteAsync(cancellationToken);

        var emailBody = $@"
            <p>Hi {command.OwnerFullName},</p>
            <p>Thank you for your interest in ApexBooking. We have received your request for a <strong>{plan}</strong> account for <strong>{command.BusinessName}</strong>.</p>
            <p>Our team will review your request and get back to you shortly.</p>
            <p>If you have any questions in the meantime, please contact our support team.</p>";

        await _notification.SendEmailAsync(
            command.OwnerEmail,
            "Your ApexBooking access request has been received",
            emailBody);

        return BaseResponse<Guid>.Success(request.Id.Value);
    }
}
