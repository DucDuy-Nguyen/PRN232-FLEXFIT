using FlexFit.Engagement.Repository.Data;
using System;

namespace FlexFit.Engagement.Service.Helpers;

public static class DateTimeHelper
{
    public static DateTime GetVietnamTime()
    {
        var utcNow = DateTime.UtcNow;
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        }
        catch
        {
            return utcNow.AddHours(7);
        }
    }
}

