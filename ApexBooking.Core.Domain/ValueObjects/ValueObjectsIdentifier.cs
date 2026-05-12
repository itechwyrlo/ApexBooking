using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.ValueObjects
{
   
    [NotMapped]
    public record TenantProfileId(Guid Value);
    [NotMapped]
    public record TenantSettingsId(Guid Value);
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
    public record ResourceAvailabilityScheduleId(Guid Value);
    [NotMapped]
    public record ResourceBreakPeriodId(Guid Value);
    [NotMapped]
    public record ResourceAvailabilityExceptionId(Guid Value);
    [NotMapped]
    public record ServiceId(Guid Value);
    [NotMapped]
    public record ServiceResourceId(Guid Value);
    [NotMapped]
    public record BookingId(Guid Value);
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
    public record SuperAdminId(Guid Value);
    [NotMapped]
    public record SubscriptionPlanId(Guid Value);
    [NotMapped]
    public record LocationId(Guid Value);
    [NotMapped]
    public record RefreshTokenId(Guid Value);
    [NotMapped]
    public record TenantPaymentGatewayId(Guid Value);
    [NotMapped]
    public record PlatformPaymentGatewayId(Guid Value);
    [NotMapped]
    public record TenantPaymentPolicyId(Guid Value);
    [NotMapped]
    public record GuestId(Guid Value);
    [NotMapped]
    public record GuestCancellationTokenId(Guid Value);
    [NotMapped]
    public record TenantRequestId(Guid Value);
}