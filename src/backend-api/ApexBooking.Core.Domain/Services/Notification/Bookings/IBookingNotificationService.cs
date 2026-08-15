using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Services.Notification.Bookings
{
    public interface IBookingNotificationService
    {
        Task SendThankYouEmailAsync(
            string to,
            string customerName,
            string businessName,
            string serviceName,
            string bookingReference,
            CancellationToken ct);

        Task SendBookingConfirmationEmailAsync(
            string to,
            string customerName,
            string businessName,
            string serviceName,
            string staffName,
            string bookingReference,
            DateOnly scheduledDate,
            TimeOnly scheduledStartTime,
            string qrImageUrl,
            string cancelBookingUrl,
            CancellationToken ct);

        Task SendBookingCancellationEmailAsync(
            string to,
            string customerName,
            string businessName,
            string serviceName,
            string bookingReference,
            string? refundNote,
            string? refundStatusUrl,
            CancellationToken ct);

        Task SendRefundRejectionEmailAsync(
            string to,
            string customerName,
            string businessName,
            string bookingReference,
            string rejectionReason,
            CancellationToken ct);

        Task SendRefundConfirmationEmailAsync(
            string to,
            string customerName,
            string businessName,
            string bookingReference,
            decimal amount,
            string currencyCode,
            string receiptUrl,
            CancellationToken ct);
    }
}