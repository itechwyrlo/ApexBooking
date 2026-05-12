using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.WebApi.Dtos;

public class UpdateTenantPaymentPolicyRequestDto
{
    public bool? PaymentRequired { get; set; }
    public bool? DepositOnly { get; set; }
    public DepositType? DepositType { get; set; }
    public decimal? DepositValue { get; set; }
    public decimal? RefundPercent { get; set; }
}
