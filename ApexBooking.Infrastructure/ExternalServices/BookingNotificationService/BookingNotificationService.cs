using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services.EmailNotification;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using MediatR;

namespace ApexBooking.Infrastructure.ExternalServices.BookingNotificationService
{
    public class BookingNotificationService : IBookingNotificationService
    {
        private readonly INotificationService _notificationService;
        public BookingNotificationService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task SendPendingApprovalEmailAsync(
            Booking booking,
            string businessName,
            CancellationToken cancellationToken)
        {
            var guest = booking.Guest;
            var scheduledDisplay = $"{booking.ScheduledDate:dddd, MMMM d, yyyy} at {booking.ScheduledStartTime:h:mm tt}";

            var body = $@"
            <!DOCTYPE html>
            <html>
            <head><meta charset=""utf-8""></head>
            <body style=""font-family: Arial, sans-serif; color: #1a1a1a; max-width: 600px; margin: 0 auto; padding: 24px;"">
            <h2 style=""color: #2d2d2d;"">Booking Request Received</h2>
            <p>Hi {guest.FirstName},</p>
            <p>Thank you for choosing <strong>{businessName}</strong>. We have successfully received your booking request.</p>
            <p>
                Your booking is currently <strong>awaiting manual confirmation</strong> from our team.
                A staff member will review your request and confirm your appointment as soon as possible.
                You will receive a follow-up email once your booking has been approved.
            </p>

            <table style=""width: 100%; border-collapse: collapse; margin: 24px 0;"">
                <tr>
                <td style=""padding: 8px 0; color: #666; width: 40%;"">Booking reference</td>
                <td style=""padding: 8px 0; font-weight: bold;"">{booking.BookingReference}</td>
                </tr>
                <tr>
                <td style=""padding: 8px 0; color: #666;"">Service</td>
                <td style=""padding: 8px 0;"">{booking.ServiceName}</td>
                </tr>
                <tr>
                <td style=""padding: 8px 0; color: #666;"">Staff</td>
                <td style=""padding: 8px 0;"">{booking.ResourceName}</td>
                </tr>
                <tr>
                <td style=""padding: 8px 0; color: #666;"">Date &amp; time</td>
                <td style=""padding: 8px 0;"">{scheduledDisplay}</td>
                </tr>
                <tr>
                <td style=""padding: 8px 0; color: #666;"">Duration</td>
                <td style=""padding: 8px 0;"">{booking.DurationMinutes} minutes</td>
                </tr>
                <tr>
                <td style=""padding: 8px 0; color: #666;"">Status</td>
                <td style=""padding: 8px 0; color: #d97706;""><strong>Pending approval</strong></td>
                </tr>
            </table>

            <p style=""margin-top: 32px; font-size: 12px; color: #999;"">
                Please do not reply to this email. If you have questions, contact {businessName} directly.
            </p>
            </body>
            </html>";

            await _notificationService.SendEmailAsync(
                guest.Email,
                $"Booking request received — {booking.BookingReference}",
                body);
        }

        public async Task SendConfirmationEmailAsync(
            Booking booking,
            string businessName,
            string cancellationUrl,
            int cancellationCutoffHours,
            CancellationToken cancellationToken)
        {
            var guest = booking.Guest;
            var scheduledDisplay = $"{booking.ScheduledDate:dddd, MMMM d, yyyy} at {booking.ScheduledStartTime:h:mm tt}";

            var body = $@"
            <!DOCTYPE html>
            <html>
            <head><meta charset=""utf-8""></head>
            <body style=""font-family: Arial, sans-serif; color: #1a1a1a; max-width: 600px; margin: 0 auto; padding: 24px;"">
            <h2 style=""color: #2d2d2d;"">Your booking is confirmed</h2>
            <p>Hi {guest.FirstName},</p>
            <p>Your appointment with <strong>{businessName}</strong> has been confirmed.</p>

            <table style=""width: 100%; border-collapse: collapse; margin: 24px 0;"">
                <tr>
                <td style=""padding: 8px 0; color: #666; width: 40%;"">Booking reference</td>
                <td style=""padding: 8px 0; font-weight: bold;"">{booking.BookingReference}</td>
                </tr>
                <tr>
                <td style=""padding: 8px 0; color: #666;"">Service</td>
                <td style=""padding: 8px 0;"">{booking.ServiceName}</td>
                </tr>
                <tr>
                <td style=""padding: 8px 0; color: #666;"">Staff</td>
                <td style=""padding: 8px 0;"">{booking.ResourceName}</td>
                </tr>
                <tr>
                <td style=""padding: 8px 0; color: #666;"">Date &amp; time</td>
                <td style=""padding: 8px 0;"">{scheduledDisplay}</td>
                </tr>
                <tr>
                <td style=""padding: 8px 0; color: #666;"">Duration</td>
                <td style=""padding: 8px 0;"">{booking.DurationMinutes} minutes</td>
                </tr>
            </table>

            <p style=""margin-top: 32px; color: #555;"">Need to cancel? Use the button below. This link expires {cancellationCutoffHours} hours before your appointment.</p>

            <a href=""{cancellationUrl}""
                style=""display: inline-block; margin-top: 8px; padding: 12px 24px;
                        background-color: #dc2626; color: #ffffff; text-decoration: none;
                        border-radius: 6px; font-weight: bold;"">
                Cancel booking
            </a>

            <p style=""margin-top: 32px; font-size: 12px; color: #999;"">
                If the button does not work, copy and paste this link into your browser:<br>
                {cancellationUrl}
            </p>
            </body>
            </html>";

            await _notificationService.SendEmailAsync(
                guest.Email,
                $"Booking confirmed — {booking.BookingReference}",
                body);
        }
    }

}