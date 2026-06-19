using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.WebApi.Dtos
{
    public class UpdateTenantSettingsRequestDto
    {
        public BookingConfirmationMode? BookingConfirmationMode { get; set; }
        public int? MinAdvanceBookingHours { get; set; }
        public int? MaxAdvanceBookingDays { get; set; }
        public int? CancellationCutoffHours { get; set; }
        public CancellationPolicy? LateCancellationPolicy { get; set; }
        public bool? GuestBookingEnabled { get; set; }
        public bool? NotifyBookingConfirmed { get; set; }
        public bool? NotifyBookingCancelled { get; set; }
        public bool? NotifyBookingReminder { get; set; }
        public bool? NotifyNewCustomer { get; set; }
        public int? ReminderHoursBefore { get; set; }
    }
}