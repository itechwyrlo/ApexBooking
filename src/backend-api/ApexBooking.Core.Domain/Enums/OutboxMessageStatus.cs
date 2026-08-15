namespace ApexBooking.Core.Domain.Enums;

public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Processed,
    Failed
}
