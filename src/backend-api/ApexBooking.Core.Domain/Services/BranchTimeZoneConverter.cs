using System;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.Services
{
    // Centralizes UTC -> branch-local conversion so every lead-time/availability calculation
    // (slot generation, booking initiation, walk-in availability) agrees on the same rule instead
    // of each hardcoding its own offset.
    public static class BranchTimeZoneConverter
    {
        public static DateTime ToBranchLocalTime(DateTime utcNow, string timeZoneId)
        {
            var timeZone = ResolveTimeZone(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        }

        public static bool IsValidTimeZoneId(string timeZoneId)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return true;
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                return false;
            }
        }

        private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                throw new BusinessRuleBrokenException($"This branch's configured time zone ('{timeZoneId}') could not be resolved.");
            }
        }
    }
}
