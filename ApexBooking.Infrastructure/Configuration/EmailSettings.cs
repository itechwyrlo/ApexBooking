using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Infrastructure.Configuration
{
    public class EmailSettings
    {
        public string SenderName { get; set; } = default!;
        public string SenderEmail { get; set; } = default!;
        public string WelcomeSubject { get; set; } = "Welcome";
    }
}
