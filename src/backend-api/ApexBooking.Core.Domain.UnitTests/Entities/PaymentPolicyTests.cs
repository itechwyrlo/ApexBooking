using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using Xunit;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.UnitTests.Entities;

public class PaymentPolicyTests
{
    [Fact]
    public void NewPolicy_DefaultsRefundEnabledToFalse()
    {
        var policy = new PaymentPolicy(new TenantId(Guid.NewGuid()));

        Assert.False(policy.RefundEnabled);
    }

    [Fact]
    public void UpdatePolicy_CanEnableRefunds()
    {
        var policy = new PaymentPolicy(new TenantId(Guid.NewGuid()));

        policy.UpdatePolicy(
            PaymentRequirementType.DepositRequired,
            DepositType.Percentage,
            depositValue: 50m,
            onTimeRefundPercent: 100m,
            lateCancellationRefundPercent: 0m,
            refundReviewDeadlineDays: 7,
            refundEnabled: true);

        Assert.True(policy.RefundEnabled);
    }

    [Fact]
    public void NewPolicy_DefaultsOnTimeRefundPercentTo100AndLateTo0()
    {
        var policy = new PaymentPolicy(new TenantId(Guid.NewGuid()));

        Assert.Equal(100m, policy.OnTimeRefundPercent);
        Assert.Equal(0m, policy.LateCancellationRefundPercent);
    }
}
