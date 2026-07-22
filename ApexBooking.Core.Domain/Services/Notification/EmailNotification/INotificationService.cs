namespace ApexBooking.Core.Domain.Services.EmailNotification
{
    public interface INotificationService
    {
       Task SendEmailAsync(string to, string subject, string content);
    }
}