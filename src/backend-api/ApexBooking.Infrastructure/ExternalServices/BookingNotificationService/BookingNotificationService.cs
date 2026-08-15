using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Services.EmailNotification;
using ApexBooking.Core.Domain.Services.Notification.Bookings;

namespace ApexBooking.Infrastructure.ExternalServices.BookingNotificationService
{
    public class BookingNotificationService : IBookingNotificationService
    {
        private readonly INotificationService _notification;

        public BookingNotificationService(INotificationService notification)
        {
            _notification = notification;
        }

        // Every value interpolated into these HTML email bodies ultimately traces back to
        // tenant- or customer-supplied text (business name, customer name, a staff-entered
        // rejection reason, etc.) — HTML-encode all of it before embedding, so none of it can
        // inject markup/links into an email actually delivered to a third party.
        private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        // URLs here are backend-constructed (IAppUrlService), not directly user-supplied, but
        // still validated defensively before landing in an href/src attribute — only ever emit
        // an absolute http(s) URL, HTML-encoded for the attribute context; anything else is
        // dropped rather than trusted.
        private static string? SafeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
                return null;

            if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
                return null;

            return WebUtility.HtmlEncode(url);
        }

        public Task SendThankYouEmailAsync(
            string to,
            string customerName,
            string businessName,
            string serviceName,
            string bookingReference,
            CancellationToken ct)
        {
            var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; border-bottom: 2px solid #0d6efd; padding-bottom: 10px;'>Thank You for Visiting Us!</h2>

                <p>Hi <strong>{H(customerName)}</strong>,</p>

                <p>Thank you for choosing <strong>{H(businessName)}</strong> today. We hope you had an excellent experience with your <strong>{H(serviceName)}</strong> service!</p>

                <div style='background: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                    <p style='margin: 0; font-size: 14px; color: #555;'><strong>Appointment Tracking Reference:</strong></p>
                    <p style='margin: 5px 0 0 0; font-size: 18px; font-weight: bold; color: #0d6efd; letter-spacing: 1px;'>{H(bookingReference)}</p>
                </div>

                <p>Your support means a lot to our team. If you have any feedback or would like to book your next session ahead of time, feel free to visit our online customer storefront booking portal.</p>

                <p style='margin-top: 30px; font-size: 14px; color: #777;'>
                    Best regards,<br>
                    The Team at <strong>{H(businessName)}</strong>
                </p>
                <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
                <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
            </div>";

            return _notification.SendEmailAsync(
                to: to,
                subject: $"Thank you for your visit! — {businessName}",
                content: body
            );
        }

        public Task SendBookingConfirmationEmailAsync(
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
            CancellationToken ct)
        {
            var formattedDate = scheduledDate.ToString("MMMM d, yyyy");
            var formattedTime = scheduledStartTime.ToString("h:mm tt");
            var safeQrImageUrl = SafeUrl(qrImageUrl);
            var safeCancelBookingUrl = SafeUrl(cancelBookingUrl);

            var qrBlock = safeQrImageUrl is null
                ? string.Empty
                : $@"
                <div style='text-align: center; margin: 20px 0;'>
                    <img src='{safeQrImageUrl}' alt='Boarding pass QR code' width='160' height='160' style='width:160px;height:160px;margin:0 0 8px 0;' />
                    <p style='margin:0;font-size:13px;color:#555;'>Show this at check-in — or just give us your name, we can look you up too.</p>
                </div>";

            var cancelLinkBlock = safeCancelBookingUrl is null
                ? string.Empty
                : $@"
                <div style='text-align: center; margin: 20px 0;'>
                    <a href='{safeCancelBookingUrl}' style='display:inline-block;padding:10px 20px;border:1px solid #d33;border-radius:6px;color:#d33;text-decoration:none;font-weight:bold;'>Cancel this booking</a>
                </div>";

            var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; border-bottom: 2px solid #0d6efd; padding-bottom: 10px;'>Your Appointment is Confirmed!</h2>

                <p>Hi <strong>{H(customerName)}</strong>,</p>

                <p>Your <strong>{H(serviceName)}</strong> appointment with <strong>{H(businessName)}</strong> is confirmed. Here are the details:</p>

                <div style='background: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                    <p style='margin: 0 0 8px 0;'><strong>Date:</strong> {H(formattedDate)}</p>
                    <p style='margin: 0 0 8px 0;'><strong>Time:</strong> {H(formattedTime)}</p>
                    <p style='margin: 0 0 8px 0;'><strong>With:</strong> {H(staffName)}</p>
                    <p style='margin: 0; font-size: 14px; color: #555;'><strong>Appointment Tracking Reference:</strong></p>
                    <p style='margin: 5px 0 0 0; font-size: 18px; font-weight: bold; color: #0d6efd; letter-spacing: 1px;'>{H(bookingReference)}</p>
                </div>

                {qrBlock}

                <p>We look forward to seeing you. Need to make a change?</p>

                {cancelLinkBlock}

                <p style='margin-top: 30px; font-size: 14px; color: #777;'>
                    Best regards,<br>
                    The Team at <strong>{H(businessName)}</strong>
                </p>
                <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
                <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
            </div>";

            return _notification.SendEmailAsync(
                to: to,
                subject: $"Appointment Confirmed — {businessName}",
                content: body
            );
        }

        public Task SendBookingCancellationEmailAsync(
            string to,
            string customerName,
            string businessName,
            string serviceName,
            string bookingReference,
            string? refundNote,
            string? refundStatusUrl,
            CancellationToken ct)
        {
            var refundBlock = string.IsNullOrWhiteSpace(refundNote)
                ? string.Empty
                : $@"
                <div style='background: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                    <p style='margin: 0;'>{H(refundNote)}</p>
                </div>";

            var safeRefundStatusUrl = SafeUrl(refundStatusUrl);
            var refundStatusLinkBlock = safeRefundStatusUrl is null
                ? string.Empty
                : $@"
                <div style='text-align: center; margin: 20px 0;'>
                    <a href='{safeRefundStatusUrl}' style='display:inline-block;padding:10px 20px;border:1px solid #0d6efd;border-radius:6px;color:#0d6efd;text-decoration:none;font-weight:bold;'>Check your refund status</a>
                </div>";

            var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; border-bottom: 2px solid #d33; padding-bottom: 10px;'>Your Appointment was Cancelled</h2>

                <p>Hi <strong>{H(customerName)}</strong>,</p>

                <p>Your <strong>{H(serviceName)}</strong> appointment with <strong>{H(businessName)}</strong> has been cancelled.</p>

                <div style='background: #f8f9fa; border-left: 4px solid #d33; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                    <p style='margin: 0; font-size: 14px; color: #555;'><strong>Appointment Tracking Reference:</strong></p>
                    <p style='margin: 5px 0 0 0; font-size: 18px; font-weight: bold; color: #d33; letter-spacing: 1px;'>{H(bookingReference)}</p>
                </div>
                {refundBlock}
                {refundStatusLinkBlock}
                <p>If you'd like to book again, feel free to visit our online booking page any time.</p>

                <p style='margin-top: 30px; font-size: 14px; color: #777;'>
                    Best regards,<br>
                    The Team at <strong>{H(businessName)}</strong>
                </p>
                <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
                <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
            </div>";

            return _notification.SendEmailAsync(
                to: to,
                subject: $"Appointment Cancelled — {businessName}",
                content: body
            );
        }

        public Task SendRefundRejectionEmailAsync(
            string to,
            string customerName,
            string businessName,
            string bookingReference,
            string rejectionReason,
            CancellationToken ct)
        {
            var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; border-bottom: 2px solid #d33; padding-bottom: 10px;'>About Your Refund</h2>

                <p>Hi <strong>{H(customerName)}</strong>,</p>

                <p>After review, the refund for your appointment with <strong>{H(businessName)}</strong> was not approved.</p>

                <div style='background: #f8f9fa; border-left: 4px solid #d33; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                    <p style='margin: 0 0 8px 0; font-size: 14px; color: #555;'><strong>Appointment Tracking Reference:</strong></p>
                    <p style='margin: 0 0 12px 0; font-size: 18px; font-weight: bold; color: #d33; letter-spacing: 1px;'>{H(bookingReference)}</p>
                    <p style='margin: 0; font-size: 14px; color: #555;'><strong>Reason:</strong></p>
                    <p style='margin: 5px 0 0 0;'>{H(rejectionReason)}</p>
                </div>

                <p>If you have questions about this decision, please contact the business directly.</p>

                <p style='margin-top: 30px; font-size: 14px; color: #777;'>
                    Best regards,<br>
                    The Team at <strong>{H(businessName)}</strong>
                </p>
                <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
                <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
            </div>";

            return _notification.SendEmailAsync(
                to: to,
                subject: $"About your refund — {businessName}",
                content: body
            );
        }

        public Task SendRefundConfirmationEmailAsync(
            string to,
            string customerName,
            string businessName,
            string bookingReference,
            decimal amount,
            string currencyCode,
            string receiptUrl,
            CancellationToken ct)
        {
            var safeReceiptUrl = SafeUrl(receiptUrl);
            var receiptBlock = safeReceiptUrl is null
                ? string.Empty
                : $@"
                <div style='text-align: center; margin: 20px 0;'>
                    <a href='{safeReceiptUrl}' style='display:inline-block;padding:10px 20px;border:1px solid #198754;border-radius:6px;color:#198754;text-decoration:none;font-weight:bold;'>View your refund receipt</a>
                </div>";

            var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; border-bottom: 2px solid #198754; padding-bottom: 10px;'>Your Refund Was Sent</h2>

                <p>Hi <strong>{H(customerName)}</strong>,</p>

                <p>Your refund for the appointment with <strong>{H(businessName)}</strong> has been sent.</p>

                <div style='background: #f8f9fa; border-left: 4px solid #198754; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                    <p style='margin: 0 0 8px 0; font-size: 14px; color: #555;'><strong>Appointment Tracking Reference:</strong></p>
                    <p style='margin: 0 0 12px 0; font-size: 18px; font-weight: bold; color: #198754; letter-spacing: 1px;'>{H(bookingReference)}</p>
                    <p style='margin: 0; font-size: 14px; color: #555;'><strong>Amount:</strong></p>
                    <p style='margin: 5px 0 0 0;'>{amount:0.00} {H(currencyCode)}</p>
                </div>
                {receiptBlock}
                <p>If you have questions, please contact the business directly.</p>

                <p style='margin-top: 30px; font-size: 14px; color: #777;'>
                    Best regards,<br>
                    The Team at <strong>{H(businessName)}</strong>
                </p>
                <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
                <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
            </div>";

            return _notification.SendEmailAsync(
                to: to,
                subject: $"Your refund was sent — {businessName}",
                content: body
            );
        }
    }
}
