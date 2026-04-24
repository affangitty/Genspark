namespace BusBooking.Domain;

/// <summary>
/// Journey dates map to PostgreSQL <c>timestamp with time zone</c>; Npgsql rejects
/// <see cref="DateTimeKind.Unspecified"/> when sending values. We treat journey as a calendar day
/// and store/query it as UTC midnight of that day.
/// </summary>
public static class JourneyDateUtc
{
    public static DateTime ToUtcCalendarStart(DateTime journeyDate)
    {
        var cal = journeyDate.Date;
        return new DateTime(cal.Year, cal.Month, cal.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>Exclusive upper bound for the journey calendar day; always UTC so Npgsql never sees mixed kinds in ranges.</summary>
    public static DateTime ToUtcCalendarEndExclusive(DateTime journeyDate)
    {
        var start = ToUtcCalendarStart(journeyDate);
        var end = start.AddDays(1);
        return end.Kind == DateTimeKind.Utc
            ? end
            : DateTime.SpecifyKind(end, DateTimeKind.Utc);
    }

    public static (DateTime StartUtc, DateTime EndExclusiveUtc) UtcDayBounds(DateTime journeyDate)
    {
        var start = ToUtcCalendarStart(journeyDate);
        return (start, ToUtcCalendarEndExclusive(journeyDate));
    }
}
