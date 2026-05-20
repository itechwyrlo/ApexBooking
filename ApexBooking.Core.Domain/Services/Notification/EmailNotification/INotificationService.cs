using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Services.EmailNotification
{
    public interface INotificationService
    {
       Task SendEmailAsync(string to, string subject, string content);
    }
}