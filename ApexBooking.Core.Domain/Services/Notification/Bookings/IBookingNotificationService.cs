using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Domain.Services.Notification.Bookings
{
    public interface IBookingNotificationService
    {
        Task SendConfirmationEmailAsync(
            Booking booking,
            string businessName,
            string cancellationUrl,
            int cancellationCutoffHours,
            CancellationToken cancellationToken);

        Task SendPendingApprovalEmailAsync(
            Booking booking,
            string businessName,
            CancellationToken cancellationToken);
    }
}