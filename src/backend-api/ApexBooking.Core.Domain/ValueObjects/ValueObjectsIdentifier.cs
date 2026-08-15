using System.ComponentModel.DataAnnotations.Schema;

namespace ApexBooking.Core.Domain.ValueObjects
{
   
    [NotMapped]
    public record TenantProfileId(Guid Value);
    [NotMapped]
    public record BookingPolicyId(Guid Value);
    [NotMapped]
    public record UserId(Guid Value);
    [NotMapped]
    public record UserProfileId(Guid Value);
    [NotMapped]
    public record UserResourceAssignmentId(Guid Value);
    [NotMapped]
    public record PasswordResetTokenId(Guid Value);
    [NotMapped]
    public record ResourceId(Guid Value);
    [NotMapped]
    public record StaffAvailabilityScheduleId(Guid Value);
    [NotMapped]
    public record StaffBreakPeriodId(Guid Value);
    [NotMapped]
    public record StaffAvailabilityExceptionId(Guid Value);
    [NotMapped]
    public record ServiceId(Guid Value);
    [NotMapped]
    public record ServiceStaffId(Guid Value);
    [NotMapped]
    public record BookingId(Guid Value);
    [NotMapped]
    public record BookingLineId(Guid Value);
    [NotMapped]
    public record BookingStatusLogId(Guid Value);
    [NotMapped]
    public record PaymentTransactionId(Guid Value);
    [NotMapped]
    public record RefundId(Guid Value);
    [NotMapped]
    public record AuditLogId(Guid Value);
    [NotMapped]
    public record NotificationLogId(Guid Value);
    [NotMapped]
    public record NotificationId(Guid Value);
    [NotMapped]
    public record SuperAdminId(Guid Value);
    [NotMapped]
    public record SubscriptionPlanId(Guid Value);
    [NotMapped]
    public record LocationId(Guid Value);
    [NotMapped]
    public record BranchId(Guid Value);
    [NotMapped]
    public record RefreshTokenId(Guid Value);
    [NotMapped]
    public record TenantPaymentGatewayId(Guid Value);
    [NotMapped]
    public record PlatformPaymentGatewayId(Guid Value);
    [NotMapped]
    public record PaymentPolicyId(Guid Value);
    [NotMapped]
    public record BookingPaymentId(Guid Value);
    [NotMapped]
    public record TenantPaymentCredentialId(Guid Value);
    [NotMapped]
    public record TenantSubscriptionId(Guid Value);
    [NotMapped]
    public record TenantPaymentAccountId(Guid Value);
    [NotMapped]
    public record GuestId(Guid Value);
    [NotMapped]
    public record CustomerId(Guid Value);
    [NotMapped]
    public record BookingCancellationTokenId(Guid Value);
    [NotMapped]
    public record TenantRequestId(Guid Value);
    [NotMapped]
    public record BusinessId(Guid Value);
    [NotMapped]
    public record BusinessProfileId(Guid Value);
    [NotMapped]
    public record SpecialHoursEntryId(Guid Value);
    [NotMapped]
    public record StaffBreakId(Guid Value);
    [NotMapped]
    public record StaffTimeOffRequestId(Guid Value);
    [NotMapped]
    public record TenantMemberId(Guid Value);
    [NotMapped]
    public record TenantRegistrationRequestId(Guid Value);
    [NotMapped]
    public record SuperAdminRefreshTokenId(Guid Value);
    [NotMapped]
    public record FcmTokenId(Guid Value);
}