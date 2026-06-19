using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Settings.Commands.UpdateTenantSettings;

internal sealed class UpdateTenantSettingsHandler
    : ICommandHandler<UpdateTenantSettingsCommand, TenantSettingsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _contextService;

    public UpdateTenantSettingsHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
    }

    public async Task<TenantSettingsDto> Handle(
        UpdateTenantSettingsCommand command,
        CancellationToken ct)
    {
        var tenantId = _contextService.GetCurrentTenantId();
        var tenant = await _unitOfWork.TenantRepository.GetAsync(
            predicate: t => t.TenantId == tenantId,
            includes: t => t.TenantSettings
        );

        if (tenant?.TenantSettings is null)
            throw new NotFoundException("Tenant settings not found.");

        tenant.TenantSettings.UpdateSettings(
            bookingConfirmationMode: command.BookingConfirmationMode,
            cancellationCutoffHours: command.CancellationCutoffHours,
            minAdvanceBookingHours: command.MinAdvanceBookingHours,
            maxAdvanceBookingDays: command.MaxAdvanceBookingDays,
            lateCancellationPolicy: command.LateCancellationPolicy,
            guestBookingEnabled: command.GuestBookingEnabled,
            notifyBookingConfirmed: command.NotifyBookingConfirmed,
            notifyBookingCancelled: command.NotifyBookingCancelled,
            notifyBookingReminder: command.NotifyBookingReminder,
            notifyNewCustomer: command.NotifyNewCustomer,
            reminderHoursBefore: command.ReminderHoursBefore);

        await _unitOfWork.CompleteAsync(ct);

        return tenant.TenantSettings.ToSettingsDto();
    }
}