using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Domain.Services.Paymongo
{
    public record PayMongoRefundResult(
        string RefundId,
        RefundStatus Status
    );
}
