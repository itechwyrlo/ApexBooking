using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Enums
{
    public enum BookingStatus
    {
        PendingPayment,
        Pending,
        Confirmed,
        Cancelled,
        Completed,
        NoShow
    }
}