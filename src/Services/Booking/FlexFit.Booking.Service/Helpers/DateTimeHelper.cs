using System;

namespace FlexFit.Booking.Service.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime GetVietnamTime()
        {
            string zoneId = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh";
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
        }
    }
}
