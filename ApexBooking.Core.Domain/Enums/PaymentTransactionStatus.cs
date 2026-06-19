using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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