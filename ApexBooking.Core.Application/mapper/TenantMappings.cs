using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.mapper;

public static class TenantMappings
{
    public static TenantSettingsDto ToSettingsDto(this TenantSettings settings) => new(
        BookingConfirmationMode: settings.BookingConfirmationMode.ToString(),
        MinAdvanceBookingHours: settings.MinAdvanceBookingHours,
        MaxAdvanceBookingDays: settings.MaxAdvanceBookingDays,
        CancellationCutoffHours: settings.CancellationCutoffHours,
        LateCancellationPolicy: settings.LateCancellationPolicy.ToString(),
        GuestBookingEnabled: settings.GuestBookingEnabled,
        NotifyBookingConfirmed: settings.NotifyBookingConfirmed,
        NotifyBookingCancelled: settings.NotifyBookingCancelled,
        NotifyBookingReminder: settings.NotifyBookingReminder,
        NotifyNewCustomer: settings.NotifyNewCustomer,
        ReminderHoursBefore: settings.ReminderHoursBefore);

    public static TenantPaymentPolicyDto ToPaymentPolicyDto(this TenantPaymentPolicy policy) => new(
        PaymentRequired: policy.PaymentRequired,
        DepositOnly: policy.DepositOnly,
        DepositType: policy.DepositType.ToString(),
        DepositValue: policy.DepositValue,
        RefundPercent: policy.RefundPercent);

    public static TenantPaymentPolicyDto DefaultPaymentPolicyDto() => new(
        PaymentRequired: false,
        DepositOnly: false,
        DepositType: "Percentage",
        DepositValue: 0m,
        RefundPercent: 0m);

    public static TenantRequestDto ToDto(this TenantRequest request) => new(
        request.Id.Value,
        request.BusinessName,
        request.OwnerFullName,
        request.OwnerEmail,
        request.Plan.ToString(),
        request.Status.ToString(),
        request.CreatedAt);

    public static TenantRequestDetailDto ToDetailDto(this TenantRequest request) => new(
        request.Id.Value,
        request.BusinessName,
        request.OwnerFullName,
        request.OwnerEmail,
        request.OwnerPhone,
        request.Plan.ToString(),
        request.Message,
        request.Status.ToString(),
        request.RejectionReason,
        request.CreatedAt,
        request.ReviewedAt);

    public static TenantUserDto ToTenantUserDto(this User user) => new(
        Id: user.Id,
        FullName: user.FullName,
        Email: user.Email!,
        Role: user.Role.ToString(),
        Status: user.Status.ToString());

    public static OrganizationSummaryDto ToOrganizationSummaryDto(this Tenant tenant, int? userCount = null) => new(
        Id: tenant.TenantId.Value,
        Slug: tenant.Slug,
        BusinessName: tenant.BusinessName,
        OwnerEmail: tenant.OwnerEmail,
        Status: tenant.Status.ToString(),
        UserCount: userCount ?? tenant.Users?.Count ?? 0,
        CreatedAt: tenant.CreatedAt);

    public static PlatformOverviewDto ToPlatformOverviewDto(this IEnumerable<OrganizationSummaryDto> organizations)
    {
        var orgs = organizations.ToList();
        return new PlatformOverviewDto(
            TotalOrgs: orgs.Count,
            ActiveOrgs: orgs.Count(o => o.Status == nameof(TenantStatus.Active)),
            InactiveOrgs: orgs.Count(o => o.Status != nameof(TenantStatus.Active)),
            Organizations: orgs);
    }

    public static OrganizationDetailDto ToOrganizationDetailDto(
        this Tenant tenant,
        int bookingCount,
        int serviceCount,
        int staffCount,
        int clientCount,
        IEnumerable<TenantUserDto> users)
    {
        var userList = users.ToList();
        return new OrganizationDetailDto(
            Id: tenant.TenantId.Value,
            Slug: tenant.Slug,
            BusinessName: tenant.BusinessName,
            OwnerFullName: tenant.OwnerFullName,
            OwnerEmail: tenant.OwnerEmail,
            OwnerPhone: tenant.OwnerPhone,
            Status: tenant.Status.ToString(),
            BookingCount: bookingCount,
            ServiceCount: serviceCount,
            StaffCount: staffCount,
            ClientCount: clientCount,
            UserCount: userList.Count,
            CreatedAt: tenant.CreatedAt,
            Users: userList);
    }

    public static PublicTenantDto ToPublicTenantDto(this Tenant tenant) => new(
        BusinessName: tenant.BusinessName,
        LogoUrl: tenant.TenantProfile?.LogoUrl,
        ContactEmail: tenant.TenantProfile?.ContactEmail,
        ContactPhone: tenant.TenantProfile?.ContactPhone,
        City: tenant.TenantProfile?.City,
        CountryCode: tenant.TenantProfile?.CountryCode,
        WebsiteUrl: tenant.TenantProfile?.WebsiteUrl,
        MinAdvanceBookingHours: tenant.TenantSettings?.MinAdvanceBookingHours ?? 1,
        MaxAdvanceBookingDays: tenant.TenantSettings?.MaxAdvanceBookingDays ?? 60,
        CancellationCutoffHours: tenant.TenantSettings?.CancellationCutoffHours ?? 24,
        Timezone: tenant.TenantProfile?.Timezone ?? "UTC");

    public static PlatformPaymentGatewayDto ToPlatformPaymentGatewayDto(this PlatformPaymentGateway gateway) => new(
        Id: gateway.PlatformPaymentGatewayId.Value,
        GatewayProvider: gateway.GatewayProvider,
        Mode: gateway.Mode,
        IsActive: gateway.IsActive,
        ValidatedAt: gateway.ValidatedAt);

    public static TenantPaymentGatewayStatusDto ToPaymentGatewayStatusDto(this PlatformPaymentGateway gateway) => new(
        GatewayProvider: gateway.GatewayProvider,
        Mode: gateway.Mode,
        IsActive: gateway.IsActive,
        ValidatedAt: gateway.ValidatedAt);

    public static TenantPaymentGatewayStatusDto DefaultPaymentGatewayStatusDto() =>
        new(null, null, false, null);

    public static TenantProfileDto ToProfileDto(this TenantProfile profile) => new
     (
         profile.LogoUrl,
         profile.AddressLine1,
         profile.AddressLine2,
         profile.City,
         profile.State,
         profile.PostalCode,
         profile.CountryCode,
         profile.Timezone,
         profile.CurrencyCode,
         profile.WebsiteUrl,
         profile.ContactEmail,
         profile.ContactPhone,
         profile.DateFormat,
         profile.TimeFormat,
         profile.LanguageCode
     );

}
