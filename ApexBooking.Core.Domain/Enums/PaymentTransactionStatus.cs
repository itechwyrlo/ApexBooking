namespace ApexBooking.Core.Domain.Enums
{
    public enum PaymentTransactionStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded,
        PartiallyRefunded
    }
}