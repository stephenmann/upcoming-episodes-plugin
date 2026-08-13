using System.Globalization;

namespace Jellyfin.Plugin.UpcomingEpisodes.Services;

/// <summary>
/// Builds the human readable "next episode" message for a calendar entry.
/// </summary>
public static class UpcomingEpisodeMessageBuilder
{
    /// <summary>
    /// Builds the message shown on a series.
    /// </summary>
    /// <param name="airDate">The local air date of the episode.</param>
    /// <param name="episodeNumber">The episode number within its season.</param>
    /// <param name="today">The current local date.</param>
    /// <param name="firstDayOfWeek">The first day of the week.</param>
    /// <returns>The message, for example <c>Next episode Thursday.</c>.</returns>
    public static string Build(DateTime airDate, int episodeNumber, DateTime today, DayOfWeek firstDayOfWeek)
    {
        var prefix = episodeNumber == 1 ? "Season premiere" : "Next episode";
        var when = IsInCurrentWeek(airDate.Date, today.Date, firstDayOfWeek)
            ? CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(airDate.DayOfWeek)
            : FormatLongDate(airDate);

        return string.Concat(prefix, " ", when, ".");
    }

    private static bool IsInCurrentWeek(DateTime airDate, DateTime today, DayOfWeek firstDayOfWeek)
    {
        var offset = ((int)today.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        var endOfWeek = today.AddDays(6 - offset);
        return airDate <= endOfWeek;
    }

    private static string FormatLongDate(DateTime airDate)
    {
        var month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(airDate.Month);
        return string.Concat(month, " ", airDate.Day.ToString(CultureInfo.InvariantCulture), GetOrdinalSuffix(airDate.Day));
    }

    private static string GetOrdinalSuffix(int day)
    {
        if (day is >= 11 and <= 13)
        {
            return "th";
        }

        return (day % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }
}
