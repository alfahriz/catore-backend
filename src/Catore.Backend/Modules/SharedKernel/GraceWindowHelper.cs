namespace Catore.Backend.Modules.SharedKernel;

public record MissingDateInfo(DateOnly Date, DateTime Deadline);

public static class GraceWindowHelper
{
    private static readonly TimeSpan GraceWindowDuration = TimeSpan.FromHours(48);

    // Tanggal yang di-expect punya log: dari signupDate s/d today (inklusif), keduanya dalam local date user.
    public static IReadOnlyList<MissingDateInfo> GetMissingDates(
        DateOnly signupDate,
        DateOnly today,
        HashSet<DateOnly> loggedDates,
        TimeZoneInfo timezone)
    {
        var missing = new List<MissingDateInfo>();

        for (var date = signupDate; date <= today; date = date.AddDays(1))
        {
            if (loggedDates.Contains(date)) continue;

            // Deadline: local midnight di akhir (date + 1 hari) + 48 jam, dikonversi ke UTC.
            var localMidnightAfterDate = date.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var localDeadline = localMidnightAfterDate.Add(GraceWindowDuration);
            var utcDeadline = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDeadline, DateTimeKind.Unspecified), timezone);

            missing.Add(new MissingDateInfo(date, utcDeadline));
        }

        return missing;
    }

    public static DateOnly? GetOldestMissingDate(IReadOnlyList<MissingDateInfo> missingDates)
    {
        return missingDates.Count == 0 ? null : missingDates.Min(m => m.Date);
    }

    public static bool IsExpired(MissingDateInfo missing, DateTime utcNow)
    {
        return utcNow >= missing.Deadline;
    }
}
